using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using BizzyQCU.Models.Admin;
using BizzyQCU.Models.Landingpage;

namespace BizzyQCU.Models.Admin
{
    public class AdminDb
    {
        private string connectionString = "server=localhost;database=BizzyQCU;uid=root;pwd=;";

        // ========== GET ALL USERS ==========
        public List<Users> GetAllUsers()
        {
            var users = new List<Users>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT user_id, username, email, role, is_approved, created_at FROM users ORDER BY created_at DESC";
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new Users
                            {
                                UserId = reader.GetInt32("user_id"),
                                Username = reader.GetString("username"),
                                Email = reader.GetString("email"),
                                Role = reader.GetString("role"),
                                IsApproved = reader.GetBoolean("is_approved"),
                                CreatedAt = reader.GetDateTime("created_at")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetAllUsers error: " + ex.Message);
            }
            return users;
        }

        public List<ApprovalRequests> GetAllStudentRequests()
        {
            var requests = new List<ApprovalRequests>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT ar.request_id, ar.username, ar.email, ar.role, ar.firstname, ar.lastname, 
                                  ar.student_number, ar.section, ar.contact_number, COALESCE(s.qcu_id, ar.qcu_id) AS qcu_id, ar.status, ar.submitted_at,
                                  u.is_approved
                           FROM approval_requests ar
                           LEFT JOIN users u ON u.username = ar.username
                           LEFT JOIN students s ON s.user_id = u.user_id
                           WHERE ar.role = 'student'
                           ORDER BY 
                               CASE LOWER(ar.status)
                                   WHEN 'pending' THEN 1 
                                   WHEN 'approved' THEN 2 
                                   WHEN 'rejected' THEN 3 
                                   ELSE 4
                               END, 
                               ar.submitted_at DESC";
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            requests.Add(new ApprovalRequests
                            {
                                RequestId = reader.GetInt32("request_id"),
                                Username = reader.GetString("username"),
                                Email = reader.GetString("email"),
                                Role = reader.GetString("role"),
                                Firstname = reader.IsDBNull(reader.GetOrdinal("firstname")) ? "" : reader.GetString("firstname"),
                                Lastname = reader.IsDBNull(reader.GetOrdinal("lastname")) ? "" : reader.GetString("lastname"),
                                StudentNumber = reader.IsDBNull(reader.GetOrdinal("student_number")) ? "" : reader.GetString("student_number"),
                                Section = reader.IsDBNull(reader.GetOrdinal("section")) ? "" : reader.GetString("section"),
                                ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
                                QcuId = reader.IsDBNull(reader.GetOrdinal("qcu_id")) ? null : (byte[])reader["qcu_id"],
                                Status = reader.GetString("status"),
                                SubmittedAt = reader.GetDateTime("submitted_at"),
                                IsAccountEnabled = reader.IsDBNull(reader.GetOrdinal("is_approved")) ? (bool?)null : reader.GetBoolean("is_approved")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetAllStudentRequests error: " + ex.Message);
            }
            return requests;
        }


        public List<ApprovalRequests> GetAllEnterpriseRequests()
        {
            var requests = new List<ApprovalRequests>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT ar.request_id, ar.username, ar.email, ar.role, ar.store_name, ar.store_description, 
                                  ar.contact_number, ar.gcash_number, COALESCE(ar.uploaded_document, e.uploaded_document) AS uploaded_document, ar.status, ar.submitted_at,
                                  u.is_approved
                           FROM approval_requests ar
                           LEFT JOIN users u ON u.username = ar.username
                           LEFT JOIN enterprises e ON e.user_id = u.user_id
                           WHERE ar.role = 'enterprise'
                           ORDER BY 
                               CASE LOWER(ar.status) 
                                   WHEN 'pending' THEN 1 
                                   WHEN 'approved' THEN 2 
                                   WHEN 'rejected' THEN 3 
                                   ELSE 4
                               END, 
                               ar.submitted_at DESC";
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            requests.Add(new ApprovalRequests
                            {
                                RequestId = reader.GetInt32("request_id"),
                                Username = reader.GetString("username"),
                                Email = reader.GetString("email"),
                                Role = reader.GetString("role"),
                                StoreName = reader.IsDBNull(reader.GetOrdinal("store_name")) ? "" : reader.GetString("store_name"),
                                StoreDescription = reader.IsDBNull(reader.GetOrdinal("store_description")) ? "" : reader.GetString("store_description"),
                                ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
                                GcashNumber = reader.IsDBNull(reader.GetOrdinal("gcash_number")) ? "" : reader.GetString("gcash_number"),
                                UploadedDocument = reader.IsDBNull(reader.GetOrdinal("uploaded_document")) ? null : (byte[])reader["uploaded_document"],
                                Status = reader.GetString("status"),
                                SubmittedAt = reader.GetDateTime("submitted_at"),
                                IsAccountEnabled = reader.IsDBNull(reader.GetOrdinal("is_approved")) ? (bool?)null : reader.GetBoolean("is_approved")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetAllEnterpriseRequests error: " + ex.Message);
            }
            return requests;
        }
        // ========== GET PENDING STUDENT APPROVALS ==========
        public List<ApprovalRequests> GetPendingStudentRequests()
        {
            var requests = new List<ApprovalRequests>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT request_id, username, email, role, firstname, lastname, 
                                          student_number, section, contact_number, qcu_id, status, submitted_at 
                                   FROM approval_requests 
                                   WHERE role = 'student' AND status = 'Pending' 
                                   ORDER BY submitted_at DESC";
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            requests.Add(new ApprovalRequests
                            {
                                RequestId = reader.GetInt32("request_id"),
                                Username = reader.GetString("username"),
                                Email = reader.GetString("email"),
                                Role = reader.GetString("role"),
                                Firstname = reader.IsDBNull(reader.GetOrdinal("firstname")) ? "" : reader.GetString("firstname"),
                                Lastname = reader.IsDBNull(reader.GetOrdinal("lastname")) ? "" : reader.GetString("lastname"),
                                StudentNumber = reader.IsDBNull(reader.GetOrdinal("student_number")) ? "" : reader.GetString("student_number"),
                                Section = reader.IsDBNull(reader.GetOrdinal("section")) ? "" : reader.GetString("section"),
                                ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
                                QcuId = reader.IsDBNull(reader.GetOrdinal("qcu_id")) ? null : (byte[])reader["qcu_id"],
                                Status = reader.GetString("status"),
                                SubmittedAt = reader.GetDateTime("submitted_at")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetPendingStudentRequests error: " + ex.Message);
            }
            return requests;
        }


        public List<ApprovalRequests> GetPendingEnterpriseRequests()
        {
            var requests = new List<ApprovalRequests>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT request_id, username, email, role, store_name, store_description, 
                                          contact_number, gcash_number, status, submitted_at 
                                   FROM approval_requests 
                                   WHERE role = 'enterprise' AND status = 'Pending' 
                                   ORDER BY submitted_at DESC";
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            requests.Add(new ApprovalRequests
                            {
                                RequestId = reader.GetInt32("request_id"),
                                Username = reader.GetString("username"),
                                Email = reader.GetString("email"),
                                Role = reader.GetString("role"),
                                StoreName = reader.IsDBNull(reader.GetOrdinal("store_name")) ? "" : reader.GetString("store_name"),
                                StoreDescription = reader.IsDBNull(reader.GetOrdinal("store_description")) ? "" : reader.GetString("store_description"),
                                ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
                                GcashNumber = reader.IsDBNull(reader.GetOrdinal("gcash_number")) ? "" : reader.GetString("gcash_number"),
                                Status = reader.GetString("status"),
                                SubmittedAt = reader.GetDateTime("submitted_at")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetPendingEnterpriseRequests error: " + ex.Message);
            }
            return requests;
        }

        //UPDATE USER APPROVAL 
        public bool UpdateUserApproval(int userId, bool isApproved)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE users SET is_approved = @isApproved WHERE user_id = @userId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@isApproved", isApproved ? 1 : 0);
                        cmd.Parameters.AddWithValue("@userId", userId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateUserApproval error: " + ex.Message);
                return false;
            }
        }

        public bool UpdateRequestAccountStatus(int requestId, bool isEnabled)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE users u
                                   INNER JOIN approval_requests ar ON ar.username = u.username
                                   SET u.is_approved = @isApproved
                                   WHERE ar.request_id = @requestId
                                     AND LOWER(ar.status) = 'approved'";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@isApproved", isEnabled ? 1 : 0);
                        cmd.Parameters.AddWithValue("@requestId", requestId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateRequestAccountStatus error: " + ex.Message);
                return false;
            }
        }

        //DELETE USER 
        public bool DeleteUser(int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM users WHERE user_id = @userId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DeleteUser error: " + ex.Message);
                return false;
            }
        }

        //APPROVE REQUEST
        //APPROVE REQUEST - WITH PROPER ERROR HANDLING
        public bool ApproveRequest(int requestId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Start a transaction para kung may error, mag-rollback lahat
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Step 1: Check if request exists and can be approved
                            string checkSql = "SELECT status FROM approval_requests WHERE request_id = @requestId FOR UPDATE";
                            string currentStatus = null;
                            using (var cmd = new MySqlCommand(checkSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@requestId", requestId);
                                var result = cmd.ExecuteScalar();
                                if (result == null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Request {requestId} not found");
                                    return false;
                                }
                                currentStatus = result.ToString();
                                if (!string.Equals(currentStatus, "Pending", StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(currentStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                                {
                                    System.Diagnostics.Debug.WriteLine($"Request {requestId} cannot be approved. Current status: {currentStatus}");
                                    return false;
                                }
                            }

                            // Step 2: Get the request details
                            string selectSql = "SELECT * FROM approval_requests WHERE request_id = @requestId";
                            ApprovalRequests request = null;
                            using (var cmd = new MySqlCommand(selectSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@requestId", requestId);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        request = new ApprovalRequests
                                        {
                                            Username = reader.GetString("username"),
                                            Password = reader.GetString("password"),
                                            Email = reader.GetString("email"),
                                            Role = reader.GetString("role"),
                                            Firstname = reader.IsDBNull(reader.GetOrdinal("firstname")) ? "" : reader.GetString("firstname"),
                                            Lastname = reader.IsDBNull(reader.GetOrdinal("lastname")) ? "" : reader.GetString("lastname"),
                                            Birthdate = reader.IsDBNull(reader.GetOrdinal("birthdate")) ? (DateTime?)null : reader.GetDateTime("birthdate"),
                                            StudentNumber = reader.IsDBNull(reader.GetOrdinal("student_number")) ? "" : reader.GetString("student_number"),
                                            Section = reader.IsDBNull(reader.GetOrdinal("section")) ? "" : reader.GetString("section"),
                                            ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
                                            QcuId = reader.IsDBNull(reader.GetOrdinal("qcu_id")) ? null : (byte[])reader["qcu_id"],
                                            StoreName = reader.IsDBNull(reader.GetOrdinal("store_name")) ? "" : reader.GetString("store_name"),
                                            StoreDescription = reader.IsDBNull(reader.GetOrdinal("store_description")) ? "" : reader.GetString("store_description"),
                                            GcashNumber = reader.IsDBNull(reader.GetOrdinal("gcash_number")) ? "" : reader.GetString("gcash_number"),
                                            UploadedDocument = reader.IsDBNull(reader.GetOrdinal("uploaded_document")) ? null : (byte[])reader["uploaded_document"]
                                        };
                                    }
                                }
                            }

                            if (request == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Request {requestId} not found after select");
                                return false;
                            }

                            System.Diagnostics.Debug.WriteLine($"Processing approval for {request.Role}: {request.Username}");

                            // Step 3: Insert into users table
                            string insertSql = @"INSERT INTO users (username, password, email, role, is_approved, created_at) 
                                         VALUES (@username, @password, @email, @role, 1, NOW())";
                            using (var cmd = new MySqlCommand(insertSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@username", request.Username);
                                cmd.Parameters.AddWithValue("@password", request.Password);
                                cmd.Parameters.AddWithValue("@email", request.Email);
                                cmd.Parameters.AddWithValue("@role", request.Role);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Inserted into users, rows affected: {rowsAffected}");
                            }

                            // Step 4: Get the new user_id
                            int userId = 0;
                            string getUserIdSql = "SELECT user_id FROM users WHERE username = @username";
                            using (var cmd = new MySqlCommand(getUserIdSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@username", request.Username);
                                var result = cmd.ExecuteScalar();
                                if (result == null)
                                {
                                    throw new Exception("Failed to retrieve user_id after insert");
                                }
                                userId = Convert.ToInt32(result);
                                System.Diagnostics.Debug.WriteLine($"Created user_id: {userId}");
                            }

                            // Step 5: Insert into role-specific table
                            if (request.Role == "student")
                            {
                                string insertStudentSql = @"INSERT INTO students (user_id, firstname, lastname, birthdate, student_number, section, contact_number, qcu_id) 
                                                    VALUES (@userId, @firstname, @lastname, @birthdate, @studentNumber, @section, @contact, @qcuId)";
                                using (var cmd = new MySqlCommand(insertStudentSql, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@userId", userId);
                                    cmd.Parameters.AddWithValue("@firstname", request.Firstname ?? "");
                                    cmd.Parameters.AddWithValue("@lastname", request.Lastname ?? "");
                                    if (request.Birthdate.HasValue)
                                    {
                                        cmd.Parameters.AddWithValue("@birthdate", request.Birthdate.Value.Date);
                                    }
                                    else
                                    {
                                        cmd.Parameters.AddWithValue("@birthdate", DBNull.Value);
                                    }
                                    cmd.Parameters.AddWithValue("@studentNumber", request.StudentNumber ?? "");
                                    cmd.Parameters.AddWithValue("@section", request.Section ?? "");
                                    cmd.Parameters.AddWithValue("@contact", request.ContactNumber ?? "");
                                    if (request.QcuId == null || request.QcuId.Length == 0)
                                    {
                                        cmd.Parameters.Add("@qcuId", MySqlDbType.Blob).Value = DBNull.Value;
                                    }
                                    else
                                    {
                                        cmd.Parameters.Add("@qcuId", MySqlDbType.Blob).Value = request.QcuId;
                                    }
                                    int rowsAffected = cmd.ExecuteNonQuery();
                                    System.Diagnostics.Debug.WriteLine($"Inserted into students, rows affected: {rowsAffected}");
                                }
                            }
                            else if (request.Role == "enterprise")
                            {
                                bool inserted = false;
                                try
                                {
                                    string insertEnterpriseWithDocSql = @"INSERT INTO enterprises (user_id, store_name, store_description, contact_number, gcash_number, uploaded_document, status) 
                                                               VALUES (@userId, @storeName, @storeDesc, @contact, @gcash, @uploadedDocument, 'approved')";
                                    using (var cmd = new MySqlCommand(insertEnterpriseWithDocSql, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@userId", userId);
                                        cmd.Parameters.AddWithValue("@storeName", request.StoreName ?? "");
                                        cmd.Parameters.AddWithValue("@storeDesc", request.StoreDescription ?? "");
                                        cmd.Parameters.AddWithValue("@contact", request.ContactNumber ?? "");
                                        cmd.Parameters.AddWithValue("@gcash", request.GcashNumber ?? "");
                                        if (request.UploadedDocument == null || request.UploadedDocument.Length == 0)
                                        {
                                            cmd.Parameters.Add("@uploadedDocument", MySqlDbType.Blob).Value = DBNull.Value;
                                        }
                                        else
                                        {
                                            cmd.Parameters.Add("@uploadedDocument", MySqlDbType.Blob).Value = request.UploadedDocument;
                                        }
                                        int rowsAffected = cmd.ExecuteNonQuery();
                                        inserted = rowsAffected > 0;
                                        System.Diagnostics.Debug.WriteLine($"Inserted into enterprises (with document), rows affected: {rowsAffected}");
                                    }
                                }
                                catch (MySqlException ex)
                                {
                                    // Backward compatibility for databases that do not yet have enterprises.uploaded_document
                                    if (ex.Message.IndexOf("uploaded_document", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        string insertEnterpriseLegacySql = @"INSERT INTO enterprises (user_id, store_name, store_description, contact_number, gcash_number, status) 
                                                                 VALUES (@userId, @storeName, @storeDesc, @contact, @gcash, 'approved')";
                                        using (var cmd = new MySqlCommand(insertEnterpriseLegacySql, conn, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@userId", userId);
                                            cmd.Parameters.AddWithValue("@storeName", request.StoreName ?? "");
                                            cmd.Parameters.AddWithValue("@storeDesc", request.StoreDescription ?? "");
                                            cmd.Parameters.AddWithValue("@contact", request.ContactNumber ?? "");
                                            cmd.Parameters.AddWithValue("@gcash", request.GcashNumber ?? "");
                                            int rowsAffected = cmd.ExecuteNonQuery();
                                            inserted = rowsAffected > 0;
                                            System.Diagnostics.Debug.WriteLine($"Inserted into enterprises (legacy), rows affected: {rowsAffected}");
                                        }
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }

                                if (!inserted)
                                {
                                    throw new Exception("Failed to insert enterprise during approval.");
                                }
                            }

                            // Step 6: Update approval_requests status
                            string updateSql = "UPDATE approval_requests SET status = 'Approved' WHERE request_id = @requestId";
                            using (var cmd = new MySqlCommand(updateSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@requestId", requestId);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Updated approval_requests status, rows affected: {rowsAffected}");
                            }

                            // Commit the transaction
                            transaction.Commit();
                            System.Diagnostics.Debug.WriteLine($"Request {requestId} approved successfully");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // Rollback on error
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Transaction rolled back. Error: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                            throw; // Re-throw to be caught by outer catch
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ApproveRequest error: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
                return false;
            }
        }


        public bool RejectRequest(int requestId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();


                    string checkSql = "SELECT status FROM approval_requests WHERE request_id = @requestId";
                    string currentStatus = null;
                    using (var cmd = new MySqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@requestId", requestId);
                        var result = cmd.ExecuteScalar();
                        if (result == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Request {requestId} not found");
                            return false;
                        }
                        currentStatus = result.ToString();
                        if (currentStatus != "Pending")
                        {
                            System.Diagnostics.Debug.WriteLine($"Request {requestId} is not pending. Current status: {currentStatus}");
                            return false;
                        }
                    }

                    string sql = "UPDATE approval_requests SET status = 'Rejected' WHERE request_id = @requestId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@requestId", requestId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        bool success = rowsAffected > 0;
                        System.Diagnostics.Debug.WriteLine($"RejectRequest: Request {requestId}, Rows affected: {rowsAffected}, Success: {success}");
                        return success;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RejectRequest error: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
                return false;
            }
        }

        public List<Feedback> GetFeedbacks(int? rating = null)
        {
            var feedbacks = new List<Feedback>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT feedback_id, user_id, user_type, email, contact_number, category, message, rating, status, created_at
                                   FROM feedbacks";

                    if (rating.HasValue)
                    {
                        sql += " WHERE rating = @rating";
                    }

                    sql += " ORDER BY feedback_id DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        if (rating.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@rating", rating.Value);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                feedbacks.Add(new Feedback
                                {
                                    FeedbackId = reader.GetInt32("feedback_id"),
                                    UserId = reader.GetInt32("user_id"),
                                    UserType = reader.GetString("user_type"),
                                    Email = reader.GetString("email"),
                                    ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
                                    Category = reader.IsDBNull(reader.GetOrdinal("category")) ? "" : reader.GetString("category"),
                                    Message = reader.GetString("message"),
                                    Rating = reader.GetInt32("rating"),
                                    Status = reader.GetString("status"),
                                    CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? (DateTime?)null : reader.GetDateTime("created_at")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetFeedbacks error: " + ex.Message);
            }

            return feedbacks;
        }

        public bool UpdateStudentRequestDetails(int requestId, string username, string email, string firstname, string lastname, string studentNumber, string section, string contactNumber)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        string requestSql = @"SELECT role, status, username FROM approval_requests 
                                              WHERE request_id = @requestId FOR UPDATE";
                        string role = null;
                        string status = null;
                        string oldUsername = null;
                        using (var requestCmd = new MySqlCommand(requestSql, conn, tx))
                        {
                            requestCmd.Parameters.AddWithValue("@requestId", requestId);
                            using (var reader = requestCmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    return false;
                                }
                                role = reader.GetString("role");
                                status = reader.GetString("status");
                                oldUsername = reader.GetString("username");
                            }
                        }

                        if (!string.Equals(role, "student", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        string updateRequestSql = @"UPDATE approval_requests
                                                    SET username = @username,
                                                        email = @email,
                                                        firstname = @firstname,
                                                        lastname = @lastname,
                                                        student_number = @studentNumber,
                                                        section = @section,
                                                        contact_number = @contactNumber
                                                    WHERE request_id = @requestId";
                        using (var cmd = new MySqlCommand(updateRequestSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@requestId", requestId);
                            cmd.Parameters.AddWithValue("@username", username ?? "");
                            cmd.Parameters.AddWithValue("@email", email ?? "");
                            cmd.Parameters.AddWithValue("@firstname", firstname ?? "");
                            cmd.Parameters.AddWithValue("@lastname", lastname ?? "");
                            cmd.Parameters.AddWithValue("@studentNumber", studentNumber ?? "");
                            cmd.Parameters.AddWithValue("@section", section ?? "");
                            cmd.Parameters.AddWithValue("@contactNumber", contactNumber ?? "");
                            cmd.ExecuteNonQuery();
                        }

                        if (string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase))
                        {
                            int userId = 0;
                            using (var userIdCmd = new MySqlCommand("SELECT user_id FROM users WHERE username = @oldUsername LIMIT 1", conn, tx))
                            {
                                userIdCmd.Parameters.AddWithValue("@oldUsername", oldUsername);
                                var userIdObj = userIdCmd.ExecuteScalar();
                                if (userIdObj != null && userIdObj != DBNull.Value)
                                {
                                    userId = Convert.ToInt32(userIdObj);
                                }
                            }

                            if (userId > 0)
                            {
                                using (var userCmd = new MySqlCommand("UPDATE users SET username = @username, email = @email WHERE user_id = @userId", conn, tx))
                                {
                                    userCmd.Parameters.AddWithValue("@username", username ?? "");
                                    userCmd.Parameters.AddWithValue("@email", email ?? "");
                                    userCmd.Parameters.AddWithValue("@userId", userId);
                                    userCmd.ExecuteNonQuery();
                                }

                                using (var studentCmd = new MySqlCommand(@"UPDATE students 
                                                                          SET firstname = @firstname,
                                                                              lastname = @lastname,
                                                                              student_number = @studentNumber,
                                                                              section = @section,
                                                                              contact_number = @contactNumber
                                                                          WHERE user_id = @userId", conn, tx))
                                {
                                    studentCmd.Parameters.AddWithValue("@firstname", firstname ?? "");
                                    studentCmd.Parameters.AddWithValue("@lastname", lastname ?? "");
                                    studentCmd.Parameters.AddWithValue("@studentNumber", studentNumber ?? "");
                                    studentCmd.Parameters.AddWithValue("@section", section ?? "");
                                    studentCmd.Parameters.AddWithValue("@contactNumber", contactNumber ?? "");
                                    studentCmd.Parameters.AddWithValue("@userId", userId);
                                    studentCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        tx.Commit();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateStudentRequestDetails error: " + ex.Message);
                return false;
            }
        }

        public bool UpdateEnterpriseRequestDetails(int requestId, string username, string email, string storeName, string businessType, string contactNumber, string gcashNumber)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        string requestSql = @"SELECT role, status, username FROM approval_requests 
                                              WHERE request_id = @requestId FOR UPDATE";
                        string role = null;
                        string status = null;
                        string oldUsername = null;
                        using (var requestCmd = new MySqlCommand(requestSql, conn, tx))
                        {
                            requestCmd.Parameters.AddWithValue("@requestId", requestId);
                            using (var reader = requestCmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    return false;
                                }
                                role = reader.GetString("role");
                                status = reader.GetString("status");
                                oldUsername = reader.GetString("username");
                            }
                        }

                        if (!string.Equals(role, "enterprise", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        string updateRequestSql = @"UPDATE approval_requests
                                                    SET username = @username,
                                                        email = @email,
                                                        store_name = @storeName,
                                                        store_description = @businessType,
                                                        contact_number = @contactNumber,
                                                        gcash_number = @gcashNumber
                                                    WHERE request_id = @requestId";
                        using (var cmd = new MySqlCommand(updateRequestSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@requestId", requestId);
                            cmd.Parameters.AddWithValue("@username", username ?? "");
                            cmd.Parameters.AddWithValue("@email", email ?? "");
                            cmd.Parameters.AddWithValue("@storeName", storeName ?? "");
                            cmd.Parameters.AddWithValue("@businessType", businessType ?? "");
                            cmd.Parameters.AddWithValue("@contactNumber", contactNumber ?? "");
                            cmd.Parameters.AddWithValue("@gcashNumber", gcashNumber ?? "");
                            cmd.ExecuteNonQuery();
                        }

                        if (string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase))
                        {
                            int userId = 0;
                            using (var userIdCmd = new MySqlCommand("SELECT user_id FROM users WHERE username = @oldUsername LIMIT 1", conn, tx))
                            {
                                userIdCmd.Parameters.AddWithValue("@oldUsername", oldUsername);
                                var userIdObj = userIdCmd.ExecuteScalar();
                                if (userIdObj != null && userIdObj != DBNull.Value)
                                {
                                    userId = Convert.ToInt32(userIdObj);
                                }
                            }

                            if (userId > 0)
                            {
                                using (var userCmd = new MySqlCommand("UPDATE users SET username = @username, email = @email WHERE user_id = @userId", conn, tx))
                                {
                                    userCmd.Parameters.AddWithValue("@username", username ?? "");
                                    userCmd.Parameters.AddWithValue("@email", email ?? "");
                                    userCmd.Parameters.AddWithValue("@userId", userId);
                                    userCmd.ExecuteNonQuery();
                                }

                                using (var enterpriseCmd = new MySqlCommand(@"UPDATE enterprises 
                                                                             SET store_name = @storeName,
                                                                                 store_description = @businessType,
                                                                                 contact_number = @contactNumber,
                                                                                 gcash_number = @gcashNumber
                                                                             WHERE user_id = @userId", conn, tx))
                                {
                                    enterpriseCmd.Parameters.AddWithValue("@storeName", storeName ?? "");
                                    enterpriseCmd.Parameters.AddWithValue("@businessType", businessType ?? "");
                                    enterpriseCmd.Parameters.AddWithValue("@contactNumber", contactNumber ?? "");
                                    enterpriseCmd.Parameters.AddWithValue("@gcashNumber", gcashNumber ?? "");
                                    enterpriseCmd.Parameters.AddWithValue("@userId", userId);
                                    enterpriseCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        tx.Commit();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateEnterpriseRequestDetails error: " + ex.Message);
                return false;
            }
        }

        public bool DeleteFeedback(int feedbackId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("DELETE FROM feedbacks WHERE feedback_id = @feedbackId", conn))
                    {
                        cmd.Parameters.AddWithValue("@feedbackId", feedbackId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DeleteFeedback error: " + ex.Message);
                return false;
            }
        }


        public List<ViewEnterprise> GetAllApprovedEnterprises()
        {
            var enterprises = new List<ViewEnterprise>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    
                    string enterpriseSql = "SELECT enterprise_id, user_id, store_name, rating_avg FROM enterprises WHERE status = 'approved'";

                    using (var cmd = new MySqlCommand(enterpriseSql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ent = new ViewEnterprise
                            {
                                EnterpriseId = reader.GetInt32("enterprise_id"),
                                UserId = reader.GetInt32("user_id"),
                                StoreName = reader.GetString("store_name"),
                                RatingAvg = reader.IsDBNull(reader.GetOrdinal("rating_avg")) ? 0 : reader.GetDecimal("rating_avg"),
                                Username = "",
                                Email = ""
                            };
                            enterprises.Add(ent);
                        }
                    }

                    for (int i = 0; i < enterprises.Count; i++)
                    {
                        var ent = enterprises[i];
                        string userSql = "SELECT username, email FROM users WHERE user_id = @userId";
                        using (var userCmd = new MySqlCommand(userSql, conn))
                        {
                            userCmd.Parameters.AddWithValue("@userId", ent.UserId);
                            using (var userReader = userCmd.ExecuteReader())
                            {
                                if (userReader.Read())
                                {
                                    ent.Username = userReader.GetString("username");
                                    ent.Email = userReader.GetString("email");
                                }
                                else
                                {
                                    ent.Username = "Unknown User";
                                    ent.Email = "unknown@email.com";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetAllApprovedEnterprises error: " + ex.Message);
            }
            return enterprises;
        }


        public EnterpriseDetails GetEnterpriseDetails(int enterpriseId)
        {
            EnterpriseDetails enterprise = null;

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Simple SELECT - inalis ang created_at dahil wala sa enterprises table
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT enterprise_id, user_id, store_name, store_description, contact_number, rating_avg, enterprise_type, gcash_number, status FROM enterprises WHERE enterprise_id = " + enterpriseId;

                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    enterprise = new EnterpriseDetails();
                    enterprise.EnterpriseId = reader.GetInt32("enterprise_id");
                    enterprise.UserId = reader.GetInt32("user_id");
                    enterprise.StoreName = reader.GetString("store_name");
                    enterprise.StoreDescription = reader.IsDBNull(reader.GetOrdinal("store_description")) ? "" : reader.GetString("store_description");
                    enterprise.ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number");
                    enterprise.RatingAvg = reader.IsDBNull(reader.GetOrdinal("rating_avg")) ? 0 : reader.GetDecimal("rating_avg");
                    enterprise.EnterpriseType = reader.IsDBNull(reader.GetOrdinal("enterprise_type")) ? "" : reader.GetString("enterprise_type");
                    enterprise.GcashNumber = reader.IsDBNull(reader.GetOrdinal("gcash_number")) ? "" : reader.GetString("gcash_number");
                    enterprise.Status = reader.GetString("status");
                    enterprise.CreatedAt = DateTime.Now; // Default value since wala sa database
                }

                reader.Close();

                // Kunin ang username at email
                if (enterprise != null)
                {
                    cmd.CommandText = "SELECT username, email FROM users WHERE user_id = " + enterprise.UserId;
                    reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        enterprise.Username = reader.GetString("username");
                        enterprise.Email = reader.GetString("email");
                    }
                    reader.Close();
                }
            }

            return enterprise;
        }

        public byte[] GetEnterpriseDocumentByEnterpriseId(int enterpriseId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT COALESCE(
                            e.uploaded_document,
                            (
                                SELECT ar.uploaded_document
                                FROM approval_requests ar
                                INNER JOIN users u2 ON u2.username = ar.username
                                WHERE ar.role = 'enterprise'
                                  AND u2.user_id = e.user_id
                                  AND ar.uploaded_document IS NOT NULL
                                ORDER BY ar.request_id DESC
                                LIMIT 1
                            )
                        ) AS enterprise_document
                        FROM enterprises e
                        WHERE e.enterprise_id = @enterpriseId
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        var result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            return null;
                        }
                        return (byte[])result;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetEnterpriseDocumentByEnterpriseId error: " + ex.Message);
                return null;
            }
        }

        // ========== GET PRODUCTS BY ENTERPRISE ID ==========
        public List<EnterpriseProduct> GetProductsByEnterpriseId(int enterpriseId)
        {
            var products = new List<EnterpriseProduct>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT product_id, enterprise_id, product_name, description, 
                                          price, product_image, status
                                   FROM products 
                                   WHERE enterprise_id = @enterpriseId AND status = 'active'
                                   ORDER BY product_id DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                products.Add(new EnterpriseProduct
                                {
                                    ProductId = reader.GetInt32("product_id"),
                                    EnterpriseId = reader.GetInt32("enterprise_id"),
                                    ProductName = reader.GetString("product_name"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
                                    Price = reader.GetDecimal("price"),
                                    ProductImage = reader.IsDBNull(reader.GetOrdinal("product_image"))
                                        ? ""
                                        : BuildImageDataUri((byte[])reader["product_image"]),
                                    Status = reader.GetString("status")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetProductsByEnterpriseId error: " + ex.Message);
            }
            return products;
        }

        // ========== DELETE ENTERPRISE ==========
        public bool DeleteEnterprise(int enterpriseId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Get user_id first
                            string getUserIdSql = "SELECT user_id FROM enterprises WHERE enterprise_id = @enterpriseId";
                            int userId = 0;
                            using (var cmd = new MySqlCommand(getUserIdSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                                var result = cmd.ExecuteScalar();
                                if (result != null)
                                {
                                    userId = Convert.ToInt32(result);
                                }
                            }

                            // Delete from enterprises
                            string deleteEnterpriseSql = "DELETE FROM enterprises WHERE enterprise_id = @enterpriseId";
                            using (var cmd = new MySqlCommand(deleteEnterpriseSql, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                                cmd.ExecuteNonQuery();
                            }

                            // Delete from users
                            if (userId > 0)
                            {
                                string deleteUserSql = "DELETE FROM users WHERE user_id = @userId";
                                using (var cmd = new MySqlCommand(deleteUserSql, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@userId", userId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DeleteEnterprise error: " + ex.Message);
                return false;
            }
        }



        // ========== GET SALES DATA FOR CHART ==========
        public List<ChartData> GetSalesData(int enterpriseId, int days = 7)
        {
            var salesData = new List<ChartData>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT DATE(order_date) as date, 
                                          SUM(total_amount) as total_sales,
                                          COUNT(*) as order_count
                                   FROM orders 
                                   WHERE enterprise_id = @enterpriseId 
                                   AND order_date >= DATE_SUB(CURDATE(), INTERVAL @days DAY)
                                   GROUP BY DATE(order_date)
                                   ORDER BY date ASC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        cmd.Parameters.AddWithValue("@days", days);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                salesData.Add(new ChartData
                                {
                                    Label = reader.GetDateTime("date").ToString("MMM dd"),
                                    Value = reader.GetDecimal("total_sales")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetSalesData error: " + ex.Message);
            }
            return salesData;
        }

        // ========== GET RATINGS DATA FOR CHART ==========
        public List<ChartData> GetRatingsData(int enterpriseId, int days = 7)
        {
            var ratingsData = new List<ChartData>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT DATE(f.created_at) as date, 
                                          AVG(f.rating) as avg_rating
                                   FROM feedbacks f
                                   INNER JOIN users u ON f.user_id = u.user_id
                                   INNER JOIN enterprises e ON e.user_id = u.user_id
                                   WHERE e.enterprise_id = @enterpriseId 
                                   AND f.created_at >= DATE_SUB(CURDATE(), INTERVAL @days DAY)
                                   GROUP BY DATE(f.created_at)
                                   ORDER BY date ASC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        cmd.Parameters.AddWithValue("@days", days);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ratingsData.Add(new ChartData
                                {
                                    Label = reader.GetDateTime("date").ToString("MMM dd"),
                                    Value = reader.GetDecimal("avg_rating")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetRatingsData error: " + ex.Message);
            }
            return ratingsData;
        }


        // ========== GET ALL PRODUCTS BY ENTERPRISE ID FOR LISTING ==========
        public List<ViewListing> GetProductsForListing(int enterpriseId)
        {
            var products = new List<ViewListing>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT p.product_id, p.enterprise_id, p.product_name, p.description, 
                                          p.price, p.product_image, p.status, p.is_approved, p.created_at,
                                          e.store_name, u.email, e.rating_avg, e.status as enterprise_status
                                   FROM products p
                                   INNER JOIN enterprises e ON p.enterprise_id = e.enterprise_id
                                   INNER JOIN users u ON e.user_id = u.user_id
                                   WHERE p.enterprise_id = @enterpriseId
                                   ORDER BY p.product_id DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@enterpriseId", enterpriseId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                products.Add(new ViewListing
                                {
                                    ProductId = reader.GetInt32("product_id"),
                                    EnterpriseId = reader.GetInt32("enterprise_id"),
                                    ProductName = reader.GetString("product_name"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
                                    Price = reader.GetDecimal("price"),
                                    ProductImage = reader.IsDBNull(reader.GetOrdinal("product_image"))
                                        ? ""
                                        : BuildImageDataUri((byte[])reader["product_image"]),
                                    Status = reader.GetString("status"),
                                    ApprovalState = reader.IsDBNull(reader.GetOrdinal("is_approved")) ? 0 : reader.GetInt32("is_approved"),
                                    CreatedAt = reader.GetDateTime("created_at"),
                                    StoreName = reader.GetString("store_name"),
                                    Email = reader.GetString("email"),
                                    RatingAvg = reader.IsDBNull(reader.GetOrdinal("rating_avg")) ? 0 : reader.GetDecimal("rating_avg"),
                                    EnterpriseStatus = reader.GetString("enterprise_status")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetProductsForListing error: " + ex.Message);
            }
            return products;
        }

        // ========== APPROVE PRODUCT ==========
        public bool ApproveProduct(int productId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE products SET status = 'active', is_approved = 1 WHERE product_id = @productId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@productId", productId);
                        var affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            return true;
                        }

                        // If no row changed, treat as success when already approved/active.
                        string checkSql = "SELECT status, is_approved FROM products WHERE product_id = @productId LIMIT 1";
                        using (var checkCmd = new MySqlCommand(checkSql, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@productId", productId);
                            using (var reader = checkCmd.ExecuteReader())
                            {
                                if (!reader.Read()) return false;
                                var status = reader.IsDBNull(reader.GetOrdinal("status")) ? "" : reader.GetString("status");
                                var approved = reader.IsDBNull(reader.GetOrdinal("is_approved")) ? 0 : reader.GetInt32("is_approved");
                                return string.Equals(status, "active", StringComparison.OrdinalIgnoreCase) && approved == 1;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Backward compatibility for DBs without is_approved column
                if (ex.Message.IndexOf("is_approved", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        using (var conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string legacySql = "UPDATE products SET status = 'active' WHERE product_id = @productId";
                            using (var cmd = new MySqlCommand(legacySql, conn))
                            {
                                cmd.Parameters.AddWithValue("@productId", productId);
                                var affected = cmd.ExecuteNonQuery();
                                if (affected > 0)
                                {
                                    return true;
                                }

                                string checkSql = "SELECT status FROM products WHERE product_id = @productId LIMIT 1";
                                using (var checkCmd = new MySqlCommand(checkSql, conn))
                                {
                                    checkCmd.Parameters.AddWithValue("@productId", productId);
                                    var result = checkCmd.ExecuteScalar();
                                    return result != null && string.Equals(result.ToString(), "active", StringComparison.OrdinalIgnoreCase);
                                }
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        System.Diagnostics.Debug.WriteLine("ApproveProduct legacy fallback error: " + innerEx.Message);
                        return false;
                    }
                }

                System.Diagnostics.Debug.WriteLine("ApproveProduct MySQL error: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ApproveProduct error: " + ex.Message);
                return false;
            }
        }

        private string BuildImageDataUri(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";

            string mime = "image/jpeg";
            if (bytes.Length >= 4)
            {
                if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) mime = "image/png";
                else if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) mime = "image/jpeg";
                else if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) mime = "image/gif";
                else if (bytes[0] == 0x42 && bytes[1] == 0x4D) mime = "image/bmp";
                else if (bytes.Length > 11 &&
                    bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                    bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) mime = "image/webp";
            }

            return "data:" + mime + ";base64," + Convert.ToBase64String(bytes);
        }

        // ========== REMOVE PRODUCT ==========
        public bool RemoveProduct(int productId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE products SET status = 'inactive', is_approved = -1 WHERE product_id = @productId";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@productId", productId);
                        var affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            return true;
                        }

                        string checkSql = "SELECT status, is_approved FROM products WHERE product_id = @productId LIMIT 1";
                        using (var checkCmd = new MySqlCommand(checkSql, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@productId", productId);
                            using (var reader = checkCmd.ExecuteReader())
                            {
                                if (!reader.Read()) return false;
                                var status = reader.IsDBNull(reader.GetOrdinal("status")) ? "" : reader.GetString("status");
                                var approval = reader.IsDBNull(reader.GetOrdinal("is_approved")) ? 0 : reader.GetInt32("is_approved");
                                return string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase) && approval == -1;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Backward compatibility for DBs without is_approved column
                if (ex.Message.IndexOf("is_approved", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        using (var conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string legacySql = "UPDATE products SET status = 'inactive' WHERE product_id = @productId";
                            using (var cmd = new MySqlCommand(legacySql, conn))
                            {
                                cmd.Parameters.AddWithValue("@productId", productId);
                                return cmd.ExecuteNonQuery() > 0;
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        System.Diagnostics.Debug.WriteLine("RemoveProduct legacy fallback error: " + innerEx.Message);
                        return false;
                    }
                }

                System.Diagnostics.Debug.WriteLine("RemoveProduct MySQL error: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RemoveProduct error: " + ex.Message);
                return false;
            }
        }
    }
}

