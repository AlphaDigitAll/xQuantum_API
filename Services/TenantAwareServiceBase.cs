using Npgsql;
using xQuantum_API.Interfaces;
using xQuantum_API.Models.Common;

namespace xQuantum_API.Services
{
    /// <summary>
    /// Base class for all tenant-aware services that need database access.
    /// Provides reusable connection management, error handling, and logging for tenant-scoped operations.
    /// Eliminates boilerplate code in service methods by handling common patterns.
    /// </summary>
    public abstract class TenantAwareServiceBase
    {
        protected readonly IConnectionStringManager ConnectionManager;
        protected readonly ITenantService TenantService;
        protected readonly ILogger Logger;

        protected TenantAwareServiceBase(
            IConnectionStringManager connectionManager,
            ITenantService tenantService,
            ILogger logger)
        {
            ConnectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            TenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a tenant-scoped database operation with automatic connection management and error handling.
        /// Handles connection string retrieval, connection lifecycle, and comprehensive error handling.
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="orgId">The organization ID (tenant identifier)</param>
        /// <param name="operation">The database operation to execute with the opened connection</param>
        /// <param name="operationName">A descriptive name for the operation (used in logging)</param>
        /// <returns>ApiResponse containing the result or error information</returns>
        protected async Task<ApiResponse<T>> ExecuteTenantOperation<T>(
            string orgId,
            Func<NpgsqlConnection, Task<T>> operation,
            string operationName)
        {
            NpgsqlConnection? conn = null;

            try
            {
                // Get tenant-specific connection string (cached for performance)
                var connectionString = await ConnectionManager.GetOrAddConnectionStringAsync(orgId,
                    async () => await TenantService.GetTenantConnectionStringAsync(orgId));

                // Create and open connection
                conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                // Execute the tenant-specific operation
                var result = await operation(conn);

                Logger.LogDebug("{Operation} completed successfully for OrgId: {OrgId}",
                    operationName, orgId);

                return ApiResponse<T>.Ok(result, $"{operationName} completed successfully.");
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23505") // Unique constraint violation
            {
                Logger.LogWarning(pgEx,
                    "{Operation} failed - duplicate entry for OrgId: {OrgId}",
                    operationName, orgId);
                return ApiResponse<T>.Fail("A record with this information already exists.");
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23503") // Foreign key violation
            {
                Logger.LogWarning(pgEx,
                    "{Operation} failed - foreign key constraint violation for OrgId: {OrgId}",
                    operationName, orgId);
                return ApiResponse<T>.Fail("Invalid reference data provided. Please check your input.");
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23502") // NOT NULL violation
            {
                Logger.LogWarning(pgEx,
                    "{Operation} failed - required field missing for OrgId: {OrgId}",
                    operationName, orgId);
                return ApiResponse<T>.Fail("Required field is missing.");
            }
            catch (PostgresException pgEx) when (pgEx.SqlState.StartsWith("53")) // Insufficient resources
            {
                Logger.LogError(pgEx,
                    "{Operation} failed - database resource issue for OrgId: {OrgId}. SqlState: {SqlState}",
                    operationName, orgId, pgEx.SqlState);
                return ApiResponse<T>.Fail("Database temporarily unavailable. Please try again in a moment.");
            }
            catch (NpgsqlException npgEx) when (npgEx.IsTransient)
            {
                Logger.LogError(npgEx,
                    "{Operation} failed - transient database error for OrgId: {OrgId}",
                    operationName, orgId);
                return ApiResponse<T>.Fail("Database connection issue. Please try again.");
            }
            catch (TimeoutException tex)
            {
                Logger.LogError(tex,
                    "{Operation} timed out for OrgId: {OrgId}",
                    operationName, orgId);
                return ApiResponse<T>.Fail("Operation timed out. Please try again or contact support if the issue persists.");
            }
            catch (InvalidOperationException ioEx) when (ioEx.Message.Contains("Organization not found"))
            {
                Logger.LogWarning(ioEx,
                    "{Operation} failed - invalid organization OrgId: {OrgId}",
                    operationName, orgId);
                return ApiResponse<T>.Fail("Invalid tenant. Please contact support.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "{Operation} failed for OrgId: {OrgId}",
                    operationName, orgId);
                return ApiResponse<T>.Fail($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Ensure connection is properly disposed
                if (conn != null)
                {
                    await conn.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Executes a tenant-scoped operation that returns a boolean result indicating success.
        /// Convenience method for operations like INSERT, UPDATE, DELETE that return row counts.
        /// </summary>
        /// <param name="orgId">The organization ID (tenant identifier)</param>
        /// <param name="operation">The database operation that returns the number of affected rows</param>
        /// <param name="operationName">A descriptive name for the operation (used in logging)</param>
        /// <returns>ApiResponse containing true if rows were affected, false otherwise</returns>
        protected async Task<ApiResponse<bool>> ExecuteTenantBoolOperation(
            string orgId,
            Func<NpgsqlConnection, Task<int>> operation,
            string operationName)
        {
            var result = await ExecuteTenantOperation(orgId, async conn =>
            {
                var rowsAffected = await operation(conn);
                return rowsAffected > 0;
            }, operationName);

            return result;
        }

        /// <summary>
        /// Executes a tenant-scoped operation within a transaction.
        /// Use this for operations that require multiple database commands to succeed or fail as a unit.
        /// </summary>
        /// <typeparam name="T">The return type of the operation</typeparam>
        /// <param name="orgId">The organization ID (tenant identifier)</param>
        /// <param name="operation">The database operation to execute within a transaction</param>
        /// <param name="operationName">A descriptive name for the operation (used in logging)</param>
        /// <returns>ApiResponse containing the result or error information</returns>
        protected async Task<ApiResponse<T>> ExecuteTenantTransactionOperation<T>(
            string orgId,
            Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> operation,
            string operationName)
        {
            NpgsqlConnection? conn = null;
            NpgsqlTransaction? transaction = null;

            try
            {
                // Get tenant-specific connection string (cached for performance)
                var connectionString = await ConnectionManager.GetOrAddConnectionStringAsync(orgId,
                    async () => await TenantService.GetTenantConnectionStringAsync(orgId));

                // Create connection and begin transaction
                conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                transaction = await conn.BeginTransactionAsync();

                // Execute the tenant-specific operation
                var result = await operation(conn, transaction);

                // Commit transaction
                await transaction.CommitAsync();

                Logger.LogDebug("{Operation} transaction completed successfully for OrgId: {OrgId}",
                    operationName, orgId);

                return ApiResponse<T>.Ok(result, $"{operationName} completed successfully.");
            }
            catch (Exception ex)
            {
                // Rollback transaction on any error
                if (transaction != null)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                        Logger.LogWarning("{Operation} transaction rolled back for OrgId: {OrgId}",
                            operationName, orgId);
                    }
                    catch (Exception rollbackEx)
                    {
                        Logger.LogError(rollbackEx,
                            "{Operation} transaction rollback failed for OrgId: {OrgId}",
                            operationName, orgId);
                    }
                }

                // Handle specific exception types
                return ex switch
                {
                    PostgresException pgEx when pgEx.SqlState == "23505" =>
                        ApiResponse<T>.Fail("A record with this information already exists."),
                    PostgresException pgEx when pgEx.SqlState == "23503" =>
                        ApiResponse<T>.Fail("Invalid reference data provided."),
                    TimeoutException =>
                        ApiResponse<T>.Fail("Operation timed out. Please try again."),
                    _ => ApiResponse<T>.Fail($"An error occurred: {ex.Message}")
                };
            }
            finally
            {
                // Ensure resources are properly disposed
                if (transaction != null)
                    await transaction.DisposeAsync();
                if (conn != null)
                    await conn.DisposeAsync();
            }
        }
    }
}
