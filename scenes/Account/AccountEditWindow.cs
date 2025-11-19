using Godot;
using System;

public partial class AccountEditWindow : Window
{
	private Label _titleLabel;
	private LineEdit _usernameEdit;
	private OptionButton _authorityOption;
	private LineEdit _passwordEdit;
	private LineEdit _confirmPasswordEdit;
	private Label _createdAtValue;
	private Label _lastLoginValue;
	private Label _errorLabel;
	private Button _saveButton;
	private Button _resetButton;
	private Button _cancelButton;

	private AcceptDialog _confirmDialog;

	private DBManager.AccountInfo? _currentAccount;
	private Action? _onAccountUpdated;
	private bool _isCreateMode;

	// pending data for create confirm
	private string _pendingUsername = "";
	private int _pendingAuthority;
	private string? _pendingPasswordPlain;

	public override void _Ready()
	{
		_titleLabel          = GetNode<Label>("MarginContainer/VBoxContainer/EditAccountLabel");
		_usernameEdit        = GetNode<LineEdit>("MarginContainer/VBoxContainer/FormGrid/UsernameEdit");
		_authorityOption     = GetNode<OptionButton>("MarginContainer/VBoxContainer/FormGrid/AuthorityOption");
		_passwordEdit        = GetNode<LineEdit>("MarginContainer/VBoxContainer/FormGrid/PasswordEdit");
		_confirmPasswordEdit = GetNode<LineEdit>("MarginContainer/VBoxContainer/FormGrid/ConfirmPasswordEdit");
		_createdAtValue      = GetNode<Label>("MarginContainer/VBoxContainer/FormGrid/CreatedAtValue");
		_lastLoginValue      = GetNode<Label>("MarginContainer/VBoxContainer/FormGrid/LastLoginValue");
		_errorLabel          = GetNode<Label>("MarginContainer/VBoxContainer/FormGrid/ErrorLabel");
		_saveButton          = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/SaveButton");
		_resetButton         = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/ResetButton");
		_cancelButton        = GetNode<Button>("MarginContainer/VBoxContainer/HBoxContainer/CancelButton");

		_saveButton.Pressed   += OnSavePressed;
		_resetButton.Pressed  += OnResetPressed;
		_cancelButton.Pressed += OnCancelPressed;

		BuildAuthorityOptions();

		// simple confirm dialog created in code
		_confirmDialog = new AcceptDialog
		{
			Title = "Confirm",
			DialogText = "Create this account?"
		};
		AddChild(_confirmDialog);
		_confirmDialog.Confirmed += OnConfirmCreate;

		_usernameEdit.PlaceholderText = "Username";
		_passwordEdit.Secret = true;
		_confirmPasswordEdit.Secret = true;
	}

	private void BuildAuthorityOptions()
	{
		_authorityOption.Clear();
		_authorityOption.AddItem("Player (0)", 0);
		_authorityOption.AddItem("Mod (1)",    1);
		_authorityOption.AddItem("Admin (2)",  2);
		_authorityOption.AddItem("Owner (3)",  3);
	}

	// ---------- EDIT EXISTING ACCOUNT ----------

	public void OpenForAccount(DBManager.AccountInfo account, Action onAccountUpdated)
	{
		_isCreateMode    = false;
		_currentAccount  = account;
		_onAccountUpdated = onAccountUpdated;

		_titleLabel.Text = "Edit Account";
		_resetButton.Visible = false; // no reset for edit, just cancel

		_usernameEdit.Text = account.Username;

		// select proper authority level
		for (int i = 0; i < _authorityOption.ItemCount; i++)
		{
			if (_authorityOption.GetItemId(i) == account.AuthorityLevel)
			{
				_authorityOption.Select(i);
				break;
			}
		}

		_passwordEdit.Text        = "";
		_confirmPasswordEdit.Text = "";
		_errorLabel.Text          = "";

		_createdAtValue.Text = account.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";
		_lastLoginValue.Text = account.LastLogin?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";

		PopupCentered(new Vector2I(450, 320));
	}

	// ---------- CREATE NEW ACCOUNT ----------

	public void OpenForCreate(Action? onAccountCreated = null)
	{
		_isCreateMode     = true;
		_currentAccount   = null;
		_onAccountUpdated = onAccountCreated;

		_titleLabel.Text = "Create Account";
		_resetButton.Visible = true;

		_usernameEdit.Text        = "";
		_passwordEdit.Text        = "";
		_confirmPasswordEdit.Text = "";
		_errorLabel.Text          = "";

		// default authority (you can change this default)
		for (int i = 0; i < _authorityOption.ItemCount; i++)
		{
			if (_authorityOption.GetItemId(i) == 0) // default Player
			{
				_authorityOption.Select(i);
				break;
			}
		}

		_createdAtValue.Text = "New account";
		_lastLoginValue.Text = "Never";

		PopupCentered(new Vector2I(450, 320));
	}

	// ---------- BUTTON HANDLERS ----------

	private void OnSavePressed()
	{
		string newUsername = _usernameEdit.Text.Trim();
		int selectedIndex  = _authorityOption.Selected;
		int newAuthLevel   = _authorityOption.GetItemId(selectedIndex);

		string newPass     = _passwordEdit.Text;
		string confirmPass = _confirmPasswordEdit.Text;

		if (string.IsNullOrWhiteSpace(newUsername))
		{
			_errorLabel.Text = "Username cannot be empty.";
			return;
		}

		if (!string.IsNullOrEmpty(newPass) || !string.IsNullOrEmpty(confirmPass))
		{
			if (newPass != confirmPass)
			{
				_errorLabel.Text = "Password and confirm password do not match.";
				return;
			}

			if (newPass.Length < 6)
			{
				_errorLabel.Text = "Password should be at least 6 characters.";
				return;
			}
		}

		if (_isCreateMode)
		{
			// store pending data and show confirm dialog
			_pendingUsername      = newUsername;
			_pendingAuthority     = newAuthLevel;
			_pendingPasswordPlain = string.IsNullOrEmpty(newPass) ? null : newPass;

			if (string.IsNullOrEmpty(_pendingPasswordPlain))
			{
				_errorLabel.Text = "Password is required for new accounts.";
				return;
			}

			_confirmDialog.DialogText =
				$"Create new account '{_pendingUsername}' with role {GetAuthorityName(_pendingAuthority)}?";
			_confirmDialog.PopupCentered();
		}
		else
		{
			// EDIT MODE: update immediately
			if (_currentAccount == null)
			{
				_errorLabel.Text = "Internal error: no account loaded.";
				return;
			}

			bool ok = DBManager.UpdateAccount(
				id: _currentAccount.Id,
				newUsername: newUsername,
				newAuthorityLevel: newAuthLevel,
				newPasswordPlain: string.IsNullOrEmpty(newPass) ? null : newPass
			);

			if (!ok)
			{
				_errorLabel.Text = "Failed to update account. See logs for details.";
				return;
			}

			_onAccountUpdated?.Invoke();
			Hide();
		}
	}

	private void OnConfirmCreate()
	{
		if (string.IsNullOrEmpty(_pendingUsername) || string.IsNullOrEmpty(_pendingPasswordPlain))
		{
			_errorLabel.Text = "Internal error: missing pending data.";
			return;
		}

		bool ok = DBManager.CreateAccount(
			username: _pendingUsername,
			authorityLevel: _pendingAuthority,
			passwordPlain: _pendingPasswordPlain
		);

		if (!ok)
		{
			_errorLabel.Text = "Failed to create account. See logs for details.";
			return;
		}

		_onAccountUpdated?.Invoke();
		Hide();
	}

	private void OnResetPressed()
	{
		if (_isCreateMode)
		{
			_usernameEdit.Text        = "";
			_passwordEdit.Text        = "";
			_confirmPasswordEdit.Text = "";
			_errorLabel.Text          = "";

			// reset authority to default (Player)
			for (int i = 0; i < _authorityOption.ItemCount; i++)
			{
				if (_authorityOption.GetItemId(i) == 0)
				{
					_authorityOption.Select(i);
					break;
				}
			}
		}
		else
		{
			// In edit mode, reset just restores original values
			if (_currentAccount == null)
				return;

			_usernameEdit.Text = _currentAccount.Username;

			for (int i = 0; i < _authorityOption.ItemCount; i++)
			{
				if (_authorityOption.GetItemId(i) == _currentAccount.AuthorityLevel)
				{
					_authorityOption.Select(i);
					break;
				}
			}

			_passwordEdit.Text        = "";
			_confirmPasswordEdit.Text = "";
			_errorLabel.Text          = "";
		}
	}

	private void OnCancelPressed()
	{
		Hide();
	}

	private string GetAuthorityName(int level)
	{
		return level switch
		{
			3 => "Owner",
			2 => "Admin",
			1 => "Mod",
			_ => "Player"
		};
	}
}
