using Godot;
using System;

public partial class LoginUI : Control
{
	private LineEdit _usernameField;
	private LineEdit _passwordField;
	private CheckBox _saveUsername;
	private Label _statusLabel;
	private Button _loginButton;
	private const string ConfigPath = "user://fat_settings.cfg";
	private ConfigFile _config = new ConfigFile();


	public override void _Ready()
	{
		_usernameField = GetNode<LineEdit>("VBoxContainer/UsernameField");
		_passwordField = GetNode<LineEdit>("VBoxContainer/PasswordField");
		_saveUsername = GetNode<CheckBox>("VBoxContainer/SaveUsernameCheckBox");
		_statusLabel = GetNode<Label>("StatusLabel");
		_loginButton = GetNode<Button>("VBoxContainer/LoginButton");

	
	var err = _config.Load(ConfigPath);
	if (err == Error.Ok)
	{
		if (_config.HasSectionKey("login", "remember_username"))
		{
			_saveUsername.ButtonPressed =
				(bool)_config.GetValue("login", "remember_username");
		}

		if (_saveUsername.ButtonPressed &&
			_config.HasSectionKey("login", "saved_username"))
		{
			_usernameField.Text =
				(string)_config.GetValue("login", "saved_username");
		}
		}
		_passwordField.TextSubmitted += OnPasswordEnter;
		_loginButton.Pressed += OnLoginPressed;

		// Connect Exit Button
		GetNode<Button>("CloseButton").Pressed += () => GetTree().Quit();

		// Test DB connection at start
		bool connected = DBManager.Connect(
			host: "82.197.82.199",   // e.g., srv123.hosting-data.io
			user: "u902447017_kael",
			password: "sw0Rdb0W!",
			 database: "u902447017_FaydarkDB"
);

if (!connected)
{
	_statusLabel.Text = "❌ DB Connection Failed";
	return;
}

// Now test that FAT's accounts table works using the PING account
bool pingOk = DBManager.ValidateLogin("PING", "FATPING");

if (!pingOk)
{
	_statusLabel.Text = "❌ DB OK, but FAT PING check failed";
	return;
}

_statusLabel.Text = "✅ DB Online";
}

private void OnPasswordEnter(string newText)
{
	OnLoginPressed();
}


	private void OnLoginPressed()
	{
		string user = _usernameField.Text;
		string pass = _passwordField.Text;

		if (DBManager.ValidateLogin(user, pass))
{
	if (_saveUsername.ButtonPressed)
	{
		_config.SetValue("login", "remember_username", true);
		_config.SetValue("login", "saved_username", user);
	}
	else
	{
		_config.SetValue("login", "remember_username", false);
		_config.SetValue("login", "saved_username", "");
	}

	_config.Save(ConfigPath);

	_statusLabel.Text = "✅ Login Successful!";

	// Small delay is optional; we can jump straight to menu:
	GetTree().ChangeSceneToFile("res://scenes/AdminMenu/AdminMenu.tscn");
}
else
{
	_statusLabel.Text = "❌ Invalid login.";
}


	}
}
