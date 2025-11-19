using Godot;
using System;

public partial class AdminMenu : Control
{
	private Label _loggedInLabel;
	private Button _logoutButton;
	private Button _exitButton;
	private Button _accountToolsButton;
	private PopupMenu _accountPopup;
	private AccountListWindow _accountListWindow;

	public override void _Ready()
{
	_loggedInLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/HBoxContainer/LoggedInLabel");
	_logoutButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/HBoxContainer/LogoutButton");
	_exitButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/ExitButton");

	

	_accountToolsButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/AccountToolsButton");
	_accountPopup       = GetNode<PopupMenu>("Window/AccountToolsPopup");

	_accountPopup.Clear();
	_accountPopup.AddItem("Create", 0);
	_accountPopup.AddItem("View", 1);

	_accountToolsButton.Pressed += OnAccountToolsPressed;
	_accountPopup.IdPressed     += OnAccountPopupItemPressed;
	_logoutButton.Pressed += OnLogoutPressed;
	_exitButton.Pressed   += OnExitPressed;

	_accountListWindow = GetNode<AccountListWindow>("AccountListWindow"); 

		
		// For now, wire world/biome/account buttons just to print something.
		HookButton("Window/MarginContainer/VBoxContainer/WorldTools/HBoxContainer/WorldCreateButton", "World Create clicked");
		HookButton("Window/MarginContainer/VBoxContainer/WorldTools/HBoxContainer/WorldCopyButton", "World Copy clicked");
		HookButton("Window/MarginContainer/VBoxContainer/WorldTools/HBoxContainer/WorldModifyButton", "World Modify clicked");
		HookButton("Window/MarginContainer/VBoxContainer/WorldTools/HBoxContainer/WorldDeleteButton", "World Delete clicked");

		HookButton("Window/MarginContainer/VBoxContainer/BiomeTools/HBoxContainer/BiomeCreateButton", "Biome Create clicked");
		HookButton("Window/MarginContainer/VBoxContainer/BiomeTools/HBoxContainer/BiomeCopyButton", "Biome Copy clicked");
		HookButton("Window/MarginContainer/VBoxContainer/BiomeTools/HBoxContainer/BiomeModifyButton", "Biome Modify clicked");
		HookButton("Window/MarginContainer/VBoxContainer/BiomeTools/HBoxContainer/BiomeDeleteButton", "Biome Delete clicked");
		
	}

	private void HookButton(string path, string message)
	{
		var button = GetNode<Button>(path);
		button.Pressed += () => GD.Print(message);
	}

	private void OnLogoutPressed()
{
	GD.Print("Logout pressed, returning to Login screen.");

	// Clean up / hide any extra windows that might still be open
	if (_accountListWindow != null && _accountListWindow.IsInsideTree())
	{
		_accountListWindow.Hide();
	}

	// Defer the actual scene change to avoid input being processed on freed nodes
	CallDeferred(nameof(GoToLoginScene));
}

private void GoToLoginScene()
{
	GetTree().ChangeSceneToFile("res://scenes/login/Login.tscn");
}

	
	private void OnAccountToolsPressed()
{
	// Position popup just under the button
	var rect = _accountToolsButton.GetGlobalRect();
	_accountPopup.Position = new Vector2I(
		(int)rect.Position.X,
		(int)(rect.Position.Y + rect.Size.Y)
	);
	_accountPopup.Popup();
}

private void OnAccountPopupItemPressed(long id)
{
	switch (id)
	{
		case 0: // Create
			GD.Print("Account Tools: Create selected");
			_accountListWindow.OpenCreateAccount();
			break;

		case 1: // View
			GD.Print("Account Tools: View selected");
			var accounts = DBManager.GetAccounts();
			_accountListWindow.ShowAccounts(accounts);

			break;
	}
}



	
	private void OnExitPressed()
	{
		GetTree().Quit();
	}
}
