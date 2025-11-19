using Godot;
using System;

public partial class AdminMenu : Control
{
	private enum ToolsContext
	{
		Accounts,
		Worlds
		// later: Biomes, etc.
	}

	private ToolsContext _currentToolsContext = ToolsContext.Accounts;

	private Label _loggedInLabel;
	private Button _logoutButton;
	private Button _exitButton;

	private Button _accountToolsButton;
	private Button _worldToolsButton;        // NEW

	private PopupMenu _toolsPopup;         // shared popup for both
	private AccountListWindow _accountListWindow;

	public override void _Ready()
	{
		_loggedInLabel = GetNode<Label>("Window/MarginContainer/VBoxContainer/HBoxContainer/LoggedInLabel");
		_logoutButton  = GetNode<Button>("Window/MarginContainer/VBoxContainer/HBoxContainer/LogoutButton");
		_exitButton    = GetNode<Button>("Window/MarginContainer/VBoxContainer/ExitButton");

		_accountToolsButton = GetNode<Button>("Window/MarginContainer/VBoxContainer/AccountToolsButton");
		_worldToolsButton   = GetNode<Button>("Window/MarginContainer/VBoxContainer/WorldToolsButton"); // path must match your scene

		_toolsPopup = GetNode<PopupMenu>("Window/ToolsPopup");

		_toolsPopup.Clear();
		_toolsPopup.AddItem("Create", 0);
		_toolsPopup.AddItem("View",   1);

		_accountToolsButton.Pressed += OnAccountToolsPressed;
		_worldToolsButton.Pressed   += OnWorldToolsPressed;   // NEW

		_toolsPopup.IdPressed     += OnToolsPopupItemPressed; // RENAMED handler

		_logoutButton.Pressed += OnLogoutPressed;
		_exitButton.Pressed   += OnExitPressed;

		_accountListWindow = GetNode<AccountListWindow>("AccountListWindow");

		// Existing debug hooks for world/biome buttons (you can keep or remove)

		HookButton("Window/MarginContainer/VBoxContainer/BiomeTools/HBoxContainer/BiomeCreateButton", "Biome Create clicked");
		HookButton("Window/MarginContainer/VBoxContainer/BiomeTools/HBoxContainer/BiomeCopyButton",   "Biome Copy clicked");
		HookButton("Window/MarginContainer/VBoxContainer/BiomeTools/HBoxContainer/BiomeModifyButton", "Biome Modify clicked");
		HookButton("Window/MarginContainer/VBoxContainer/BiomeTools/HBoxContainer/BiomeDeleteButton", "Biome Delete clicked");
	}

	private void HookButton(string path, string message)
	{
		var button = GetNode<Button>(path);
		button.Pressed += () => GD.Print(message);
	}

	// ---------- LOGIN / EXIT ----------

	private void OnLogoutPressed()
	{
		GD.Print("Logout pressed, returning to Login screen.");

		if (_accountListWindow != null && _accountListWindow.IsInsideTree())
			_accountListWindow.Hide();

		CallDeferred(nameof(GoToLoginScene));
	}

	private void GoToLoginScene()
	{
		GetTree().ChangeSceneToFile("res://scenes/login/Login.tscn");
	}

	private void OnExitPressed()
	{
		GetTree().Quit();
	}

	// ---------- TOOLS POPUPS ----------

	private void OnAccountToolsPressed()
	{
		_currentToolsContext = ToolsContext.Accounts;

		var rect = _accountToolsButton.GetGlobalRect();
		_toolsPopup.Position = new Vector2I(
			(int)rect.Position.X,
			(int)(rect.Position.Y + rect.Size.Y)
		);
		_toolsPopup.Popup();
	}

	private void OnWorldToolsPressed()
	{
		_currentToolsContext = ToolsContext.Worlds;

		var rect = _worldToolsButton.GetGlobalRect();
		_toolsPopup.Position = new Vector2I(
			(int)rect.Position.X,
			(int)(rect.Position.Y + rect.Size.Y)
		);
		_toolsPopup.Popup();
	}

	private void OnToolsPopupItemPressed(long id)
	{
		switch (_currentToolsContext)
		{
			case ToolsContext.Accounts:
				HandleAccountToolsSelection(id);
				break;

			case ToolsContext.Worlds:
				HandleWorldToolsSelection(id);
				break;
		}
	}

	// ---------- ACCOUNTS TOOLS ----------

	private void HandleAccountToolsSelection(long id)
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

	// ---------- WORLDS TOOLS ----------

	private void HandleWorldToolsSelection(long id)
	{
		switch (id)
		{
			case 0: // Create
				GD.Print("World Tools: Create selected");
				// For now, jump straight into terrain editor scene.
				GetTree().ChangeSceneToFile("res://scenes/WorldTerrainEditor/WorldTerrainEditor.tscn");
				break;

			case 1: // View
				GD.Print("World Tools: View selected");
				// TODO: implement a WorldListWindow similar to AccountListWindow
				break;
		}
	}
}
