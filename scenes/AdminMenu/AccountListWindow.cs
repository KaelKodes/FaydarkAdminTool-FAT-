using Godot;
using System;
using System.Collections.Generic;

public partial class AccountListWindow : Window
{
	private VBoxContainer _accountsContainer;
	private Button _closeButton;
	private Button _editButton;
	private Button _deleteButton;

	// Map each checkbox to its account info
	private readonly Dictionary<CheckBox, DBManager.AccountInfo> _checkboxMap =
		new Dictionary<CheckBox, DBManager.AccountInfo>();

	private AccountEditWindow? _editWindow;

	// For delete confirmation
	private AcceptDialog _deleteDialog;
	private List<DBManager.AccountInfo> _pendingDelete = new List<DBManager.AccountInfo>();

	public override void _Ready()
	{
		_accountsContainer =
			GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ScrollContainer/AccountsContainer");
		_closeButton =
			GetNode<Button>("MarginContainer/VBoxContainer/CloseButton");
		_editButton =
			GetNode<Button>("MarginContainer/VBoxContainer/ButtonRow/EditButton");
		_deleteButton =
			GetNode<Button>("MarginContainer/VBoxContainer/ButtonRow/DeleteButton");

		_closeButton.Pressed += OnClosePressed;
		_editButton.Pressed  += OnEditPressed;
		_deleteButton.Pressed += OnDeletePressed;

		// Initially disabled until something is selected
		_editButton.Disabled = true;
		_deleteButton.Disabled = true;

		// Load AccountEditWindow scene
		var packed = GD.Load<PackedScene>("res://scenes/Account/AccountEditWindow.tscn");
		_editWindow = packed.Instantiate<AccountEditWindow>();
		GetTree().Root.AddChild(_editWindow);

		// Delete confirmation dialog
		_deleteDialog = new AcceptDialog
		{
			Title = "Confirm Delete"
		};
		AddChild(_deleteDialog);
		_deleteDialog.Confirmed += OnConfirmDelete;
	}

	public void ShowAccounts(List<DBManager.AccountInfo> accounts)
	{
		// Clear previous children + map
		foreach (Node child in _accountsContainer.GetChildren())
			child.QueueFree();
		_checkboxMap.Clear();

		if (accounts.Count == 0)
		{
			var noLabel = new Label { Text = "No admin accounts found." };
			_accountsContainer.AddChild(noLabel);
		}
		else
		{
			foreach (var acc in accounts)
			{
				var row = new HBoxContainer();

				var check = new CheckBox();
				check.Toggled += OnAccountChecked;
				row.AddChild(check);

				var label = new Label();
				string role = GetAuthorityName(acc.AuthorityLevel);
				label.Text = $"{acc.Username} [{role}]";
				label.HorizontalAlignment = HorizontalAlignment.Left;
				row.AddChild(label);

				_accountsContainer.AddChild(row);

				_checkboxMap[check] = acc;
			}
		}

		UpdateButtonsState();

		GD.Print($"[AccountListWindow] Showing {accounts.Count} account(s).");
		PopupCentered(new Vector2I(400, 300));
	}

	private void OnAccountChecked(bool _pressed)
	{
		UpdateButtonsState();
	}

	private void UpdateButtonsState()
	{
		int selectedCount = 0;

		foreach (var kv in _checkboxMap)
		{
			if (kv.Key.ButtonPressed)
				selectedCount++;
		}

		_editButton.Disabled = selectedCount != 1;
		_deleteButton.Disabled = selectedCount == 0;
	}

	private void OnClosePressed()
	{
		Hide();
	}

	// Helpers for when we wire up Edit/Delete later
	public List<DBManager.AccountInfo> GetSelectedAccounts()
	{
		var selected = new List<DBManager.AccountInfo>();

		foreach (var kv in _checkboxMap)
		{
			if (kv.Key.ButtonPressed)
				selected.Add(kv.Value);
		}

		return selected;
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
	
	public void OpenCreateAccount()
{
	if (_editWindow == null)
		return;

	_editWindow.OpenForCreate(() =>
	{
		var refreshed = DBManager.GetAccounts();
		ShowAccounts(refreshed);
	});
}



	private void OnEditPressed()
	{
		var selected = GetSelectedAccounts();
		if (selected.Count != 1 || _editWindow == null)
			return;

		var acc = selected[0];

		_editWindow.OpenForAccount(acc, () =>
		{
			// Refresh the list after a successful save
			var refreshed = DBManager.GetAccounts();
			ShowAccounts(refreshed);

		});
	}

	private void OnDeletePressed()
	{
		var selected = GetSelectedAccounts();
		if (selected.Count == 0)
			return;

		_pendingDelete = selected;

		// Build confirmation text
		if (selected.Count == 1)
		{
			var acc = selected[0];
			_deleteDialog.DialogText =
				$"Delete account '{acc.Username}' [{GetAuthorityName(acc.AuthorityLevel)}]?\nThis cannot be undone.";
		}
		else
		{
			_deleteDialog.DialogText =
				$"Delete {selected.Count} accounts?\nThis cannot be undone.";
		}

		_deleteDialog.PopupCentered();
	}

	private void OnConfirmDelete()
	{
		if (_pendingDelete == null || _pendingDelete.Count == 0)
			return;

		var ids = new List<int>();
		foreach (var acc in _pendingDelete)
			ids.Add(acc.Id);

		bool ok = DBManager.DeleteAccounts(ids);
		if (!ok)
		{
			GD.PrintErr("❌ Failed to delete selected accounts.");
			return;
		}

		// Refresh list after delete
		var refreshed = DBManager.GetAccounts();
		ShowAccounts(refreshed);

		_pendingDelete.Clear();
	}
}
