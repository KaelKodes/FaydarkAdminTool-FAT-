using Godot;
using System;
using System.Collections.Generic;
using System.Data;                    // For ConnectionState
using System.Security.Cryptography;
using System.Text;
using MySqlConnector;

public static class DBManager
{
	private static MySqlConnection _connection;

	// Stored connection info for reconnects
	private static string _host = "";
	private static string _user = "";
	private static string _password = "";
	private static string _database = "";
	private static uint _port = 3306;

	// ----- TYPES -----

	public sealed class AccountInfo
	{
		public int Id;
		public string Username = "";
		public int AuthorityLevel;
		public DateTime? CreatedAt;
		public DateTime? LastLogin;
	}

	// ----- CONNECTION -----

	public static bool Connect(string host, string user, string password, string database, uint port = 3306)
	{
		_host = host;
		_user = user;
		_password = password;
		_database = database;
		_port = port;

		string connStr =
			$"Server={host};Port={port};Database={database};Uid={user};Pwd={password};SslMode=Preferred;";

		try
		{
			// Close old connection if present
			if (_connection != null)
			{
				try { _connection.Dispose(); }
				catch { }
				_connection = null;
			}

			_connection = new MySqlConnection(connStr);
			_connection.Open();
			GD.Print("✅ Connected to MySQL!");
			return true;
		}
		catch (Exception e)
		{
			GD.PrintErr($"❌ DB Connection failed: {e.Message}");
			return false;
		}
	}

	private static bool EnsureConnected()
	{
		try
		{
			if (_connection != null && _connection.State == ConnectionState.Open)
				return true;
		}
		catch
		{
			// Any exception checking State will trigger reconnect path
		}

		if (string.IsNullOrEmpty(_host) || string.IsNullOrEmpty(_user) ||
			string.IsNullOrEmpty(_database))
		{
			GD.PrintErr("❌ EnsureConnected: No stored connection info.");
			return false;
		}

		GD.Print("ℹ️ Reconnecting to MySQL...");
		return Connect(_host, _user, _password, _database, _port);
	}

	// ----- AUTH -----

	public static bool ValidateLogin(string username, string password)
	{
		if (!EnsureConnected())
		{
			GD.PrintErr("❌ Cannot validate login: Not connected to DB.");
			return false;
		}

		const string query = "SELECT password_hash FROM accounts WHERE username = @username LIMIT 1;";

		try
		{
			using var cmd = new MySqlCommand(query, _connection);
			cmd.Parameters.AddWithValue("@username", username);

			using var reader = cmd.ExecuteReader();

			if (!reader.Read())
				return false;

			string storedHash = reader.GetString(0);      // from MySQL (SHA2)
			string inputHash  = HashPassword(password);   // from C# (SHA256)

			// MySQL hex vs our hex → compare case-insensitive.
			return storedHash.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
		}
		catch (Exception e)
		{
			GD.PrintErr($"❌ ValidateLogin error: {e.Message}");
			return false;
		}
	}

	// ----- ACCOUNT LISTING -----

	public static List<AccountInfo> GetAccounts()
{
	var result = new List<AccountInfo>();

	if (!EnsureConnected())
	{
		GD.PrintErr("❌ Cannot load accounts: Not connected to DB.");
		return result;
	}

	string query =
		"SELECT id, username, authority_level, created_at, last_login " +
		"FROM accounts " +
		"ORDER BY username;";

	try
	{
		using var cmd = new MySqlCommand(query, _connection);
		using var reader = cmd.ExecuteReader();

		while (reader.Read())
		{
			var info = new AccountInfo
			{
				Id             = reader.GetInt32(0),
				Username       = reader.GetString(1),
				AuthorityLevel = reader.GetInt32(2),
				CreatedAt      = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
				LastLogin      = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
			};

			result.Add(info);
		}

		GD.Print($"[DBManager] GetAccounts loaded {result.Count} row(s).");
	}
	catch (Exception e)
	{
		GD.PrintErr($"❌ GetAccounts error: {e.Message}");
	}

	return result;
}


	// ----- UTIL -----

	private static string HashPassword(string password)
	{
		using SHA256 sha = SHA256.Create();
		byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
		return BitConverter.ToString(bytes).Replace("-", "").ToLower();
	}

	// ----- UPDATE ACCOUNT -----

	public static bool UpdateAccount(int id, string newUsername, int newAuthorityLevel, string? newPasswordPlain = null)
	{
		if (!EnsureConnected())
		{
			GD.PrintErr("❌ Cannot update account: Not connected to DB.");
			return false;
		}

		if (string.IsNullOrWhiteSpace(newUsername))
		{
			GD.PrintErr("❌ Cannot update account: Username is empty.");
			return false;
		}

		string query;
		bool changePassword = !string.IsNullOrEmpty(newPasswordPlain);

		if (changePassword)
		{
			query = @"
                UPDATE accounts
                SET username = @username,
                    authority_level = @auth,
                    password_hash = @password_hash
				WHERE id = @id";
		}
		else
		{
			query = @"
                UPDATE accounts
                SET username = @username,
                    authority_level = @auth
				WHERE id = @id";
		}

		try
		{
			using var cmd = new MySqlCommand(query, _connection);
			cmd.Parameters.AddWithValue("@id", id);
			cmd.Parameters.AddWithValue("@username", newUsername);
			cmd.Parameters.AddWithValue("@auth", newAuthorityLevel);

			if (changePassword)
			{
				string newHash = HashPassword(newPasswordPlain!);
				cmd.Parameters.AddWithValue("@password_hash", newHash);
			}

			int rows = cmd.ExecuteNonQuery();
			if (rows == 0)
			{
				GD.PrintErr($"❌ UpdateAccount: No rows affected for id={id}");
				return false;
			}

			GD.Print($"[DBManager] Updated account id={id} (username={newUsername}, auth={newAuthorityLevel}, changedPassword={changePassword})");
			return true;
		}
		catch (Exception e)
		{
			GD.PrintErr($"❌ UpdateAccount error: {e.Message}");
			return false;
		}
	}

	// ----- CREATE ACCOUNT -----

	public static bool CreateAccount(string username, int authorityLevel, string passwordPlain)
	{
		if (!EnsureConnected())
		{
			GD.PrintErr("❌ Cannot create account: Not connected to DB.");
			return false;
		}

		if (string.IsNullOrWhiteSpace(username))
		{
			GD.PrintErr("❌ Cannot create account: Username is empty.");
			return false;
		}

		if (string.IsNullOrWhiteSpace(passwordPlain))
		{
			GD.PrintErr("❌ Cannot create account: Password is empty.");
			return false;
		}

		const string query = @"
            INSERT INTO accounts (username, password_hash, authority_level)
			VALUES (@username, @password_hash, @auth);";

		try
		{
			using var cmd = new MySqlCommand(query, _connection);
			cmd.Parameters.AddWithValue("@username", username);
			cmd.Parameters.AddWithValue("@auth", authorityLevel);

			string hash = HashPassword(passwordPlain);
			cmd.Parameters.AddWithValue("@password_hash", hash);

			int rows = cmd.ExecuteNonQuery();
			if (rows == 0)
			{
				GD.PrintErr("❌ CreateAccount: No rows inserted.");
				return false;
			}

			GD.Print($"[DBManager] Created account username={username}, auth={authorityLevel}");
			return true;
		}
		catch (MySqlException ex)
		{
			GD.PrintErr($"❌ CreateAccount MySql error: {ex.Message}");
			return false;
		}
		catch (Exception e)
		{
			GD.PrintErr($"❌ CreateAccount error: {e.Message}");
			return false;
		}
	}
	
	public static bool DeleteAccounts(List<int> accountIds)
{
	if (accountIds == null || accountIds.Count == 0)
	{
		GD.PrintErr("❌ DeleteAccounts called with empty id list.");
		return false;
	}

	if (!EnsureConnected())
	{
		GD.PrintErr("❌ Cannot delete accounts: Not connected to DB.");
		return false;
	}

	const string query = "DELETE FROM accounts WHERE id = @id";

	try
	{
		using var cmd = new MySqlCommand(query, _connection);
		var idParam = cmd.Parameters.Add("@id", MySqlDbType.Int32);

		int totalDeleted = 0;
		foreach (int id in accountIds)
		{
			idParam.Value = id;
			int rows = cmd.ExecuteNonQuery();
			totalDeleted += rows;
		}

		GD.Print($"[DBManager] DeleteAccounts removed {totalDeleted} row(s).");
		return true;
	}
	catch (Exception e)
	{
		GD.PrintErr($"❌ DeleteAccounts error: {e.Message}");
		return false;
	}
}

	
}
