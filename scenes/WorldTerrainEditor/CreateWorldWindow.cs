using Godot;
using System;

/*
 *  CreateWorldWindow
 *  ------------------
 *  This window gathers all world-generation parameters from the admin.
 *  When "Generate" is pressed, all values are passed to WorldTerrainEditor.
 */

public partial class CreateWorldWindow : Window
{
	// World Behavior
	private SpinBox _widthBox;
	private SpinBox _heightBox;
	private LineEdit _seedBox;
	private CheckBox _boundariesCheck;
	private CheckBox _keepSeedCheck;


	// Continents
	private SpinBox _continentsBox;
	private SpinBox _minDistanceBox;
	private SpinBox _maxDistanceBox;
	private SpinBox _sizeVarianceBox;
	private OptionButton _irregularityBox;


	// Water
	private SpinBox _waterPercentBox;
	private SpinBox _waterBodiesBox;
	private SpinBox _waterRiverChanceBox;
	private SpinBox _maxRiversPerBodyBox;

	// Mountains
	private SpinBox _mountainPercentBox;
	private SpinBox _tallMountainsBox;
	private SpinBox _mountainRiverChanceBox;
	private SpinBox _maxRiversTMBox;

	// Buttons
	private Button _generateButton;
	private Button _resetButton;
	private Button _backButton;
	
	//UI
	private Label _warningLabel;
	private Label _waterDisplay;
	private Label _landDisplay;
	private Label _mountainDisplay;



	public override void _Ready()
	{
		// WORLD BEHAVIOR
		_widthBox      = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer/WorldGrid/WidthBox");
		_heightBox     = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer/WorldGrid/HeightBox");
		_seedBox       = GetNode<LineEdit>("VBoxContainer/HBoxContainer/VBoxContainer/WorldGrid/SeedBox");
		_boundariesCheck = GetNode<CheckBox>("VBoxContainer/HBoxContainer/VBoxContainer/WorldGrid/BoundariesCheck");
		_keepSeedCheck = GetNode<CheckBox>("VBoxContainer/HBoxContainer/VBoxContainer/WorldGrid/KeepSeedCheck");


		// CONTINENTS
		_continentsBox  = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer2/ContinentGrid/ContinentBox");
		_minDistanceBox = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer2/ContinentGrid/MinDistanceBox");
		_maxDistanceBox = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer2/ContinentGrid/MaxDistanceBox");
		_sizeVarianceBox = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer2/ContinentGrid/SizeVarianceBox");
		_irregularityBox = GetNode<OptionButton>("VBoxContainer/HBoxContainer/VBoxContainer2/ContinentGrid/IrregularityBox");


		// WATER
		_waterPercentBox    = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer/WaterGrid/WaterPercentBox");
		_waterBodiesBox     = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer/WaterGrid/WaterBodiesBox");
		_waterRiverChanceBox = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer/WaterGrid/WaterRiverChanceBox");
		_maxRiversPerBodyBox = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer/WaterGrid/MaxRiverPerBodyBox");

		// MOUNTAINS
		_mountainPercentBox    = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer2/MountainGrid/MountainPercentBox");
		_tallMountainsBox      = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer2/MountainGrid/TallMountainsBox");
		_mountainRiverChanceBox = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer2/MountainGrid/MountainRiverChanceBox");
		_maxRiversTMBox         = GetNode<SpinBox>("VBoxContainer/HBoxContainer/VBoxContainer2/MountainGrid/MaxRiversTMBox");

		// BUTTONS
		_generateButton = GetNode<Button>("ButtonRow/GenerateButton");
		_resetButton    = GetNode<Button>("ButtonRow/ResetButton");
		_backButton     = GetNode<Button>("ButtonRow/BackButton");

		_generateButton.Pressed += OnGeneratePressed;
		_resetButton.Pressed    += OnResetPressed;
		_backButton.Pressed     += OnBackPressed;
		
		
		// UI
		_warningLabel = GetNode<Label>("VBoxContainer/WarningLabel");
		_warningLabel.Text = "";
		_waterDisplay   = GetNode<Label>("VBoxContainer/VBoxContainer/WaterDisplay");
		_landDisplay    = GetNode<Label>("VBoxContainer/VBoxContainer/LandDisplay");
		_mountainDisplay= GetNode<Label>("VBoxContainer/VBoxContainer/MountainDisplay");
		
		_waterPercentBox.ValueChanged += OnRatioValuesChanged;
		_mountainPercentBox.ValueChanged += OnRatioValuesChanged;
		_resetButton.Pressed += ( ) => UpdateRatioDisplay();





		SetDefaults();
		UpdateRatioDisplay();

	}
	
	


	private void SetDefaults()
	{
		_widthBox.Value = 25;
		_heightBox.Value = 25;

		if (!_keepSeedCheck.ButtonPressed)
{
	_seedBox.Text = GD.Randi().ToString();
}

		_boundariesCheck.ButtonPressed = true;

		// Example defaults (you can adjust these)
		_continentsBox.Value = 3;
		_minDistanceBox.Value = 5;
		_maxDistanceBox.Value = 20;
		_sizeVarianceBox.Value = 30;
		_irregularityBox.Selected = 3;

		_waterPercentBox.Value = 40;
		_waterBodiesBox.Value = 5;
		_waterRiverChanceBox.Value = 15;
		_maxRiversPerBodyBox.Value = 2;

		_mountainPercentBox.Value = 20;
		_tallMountainsBox.Value = 10;
		_mountainRiverChanceBox.Value = 10;
		_maxRiversTMBox.Value = 1;
	}
	
	private void UpdateRatioDisplay()
{
	int water = (int)_waterPercentBox.Value;
	int mountains = (int)_mountainPercentBox.Value;
	int land = 100 - water - mountains;

	if (land < 0)
		land = 0; // purely visual

	_waterDisplay.Text = $"{water}%";
	_mountainDisplay.Text = $"{mountains}%";
	_landDisplay.Text = $"{land}%";
}

private void OnRatioValuesChanged(double _value)
{
	UpdateRatioDisplay();
}


	private void OnResetPressed()
	{
		SetDefaults();
	}

	private void OnBackPressed()
{
	Hide(); // immediately hide the popup so it stops receiving input

	// Turn off all processing safely
	SetProcess(false);
	SetProcessInput(false);
	SetProcessUnhandledInput(false);
	SetProcessUnhandledKeyInput(false);

	// The correct, safe way to change scenes from a popup
	CallDeferred(nameof(GoBackToAdminMenu));
}

private void GoBackToAdminMenu()
{
	GetTree().ChangeSceneToFile("res://Scenes/AdminMenu/AdminMenu.tscn");
}



	private void OnGeneratePressed()
{
	int sizeVariance  = (int)_sizeVarianceBox.Value;
	int irregularity  = _irregularityBox.Selected;

	if (!ValidateInputs())
		return;
}
	
	private bool ValidateInputs()
{
	_warningLabel.Text = "";
	_generateButton.Disabled = false;

	int width = (int)_widthBox.Value;
	int height = (int)_heightBox.Value;

	// Width / Height
	if (width < 5 || height < 5)
		return Fail("World must be at least 5x5.");

	// Seed
	if (!long.TryParse(_seedBox.Text, out _))
		return Fail("Seed must be a valid number.");

	// Continents
	int continents = (int)_continentsBox.Value;
	int minDist = (int)_minDistanceBox.Value;
	int maxDist = (int)_maxDistanceBox.Value;

	if (continents < 1)
		return Fail("Continents must be at least 1.");

	if (minDist < 1 || maxDist < 1)
		return Fail("Distances must be >= 1.");

	if (maxDist <= minDist)
		return Fail("Max distance must be greater than Min distance.");

	if (maxDist > width && maxDist > height)
		return Fail("Max distance is larger than world size.");
	
		int sizeVariance = (int)_sizeVarianceBox.Value;
	
	if (sizeVariance < 0 || sizeVariance > 100)
		return Fail("Size Variance must be between 0 and 100.");

		int irregularity = _irregularityBox.Selected;  // 0–5


	// Water
	int waterPercent = (int)_waterPercentBox.Value;
	int waterBodies = (int)_waterBodiesBox.Value;

	if (waterPercent < 0 || waterPercent > 100)
		return Fail("Water % must be between 0 and 100.");

	if (waterBodies < 0)
		return Fail("Bodies of water cannot be negative.");

	if (waterBodies > width * height)
		return Fail("Too many water bodies for the world size.");

	if (waterPercent == 0 && waterBodies > 0)
		return Fail("Cannot have water bodies with 0% water.");

	// Rivers
	int waterRiverChance = (int)_waterRiverChanceBox.Value;
	if (waterRiverChance < 0 || waterRiverChance > 100)
		return Fail("Water river chance must be 0–100.");

	int maxRiverBody = (int)_maxRiversPerBodyBox.Value;
	if (maxRiverBody < 0)
		return Fail("Max rivers per body cannot be negative.");

	// Mountains
	int mountainPercent = (int)_mountainPercentBox.Value;
	if (mountainPercent < 0 || mountainPercent > 100)
		return Fail("Mountain % must be between 0 and 100.");

	if (waterPercent + mountainPercent > 100)
		return Fail("Water% + Mountain% cannot exceed 100.");

	int tallMountains = (int)_tallMountainsBox.Value;
	if (tallMountains < 0)
		return Fail("Tall mountains cannot be negative.");

	int mountainRiverChance = (int)_mountainRiverChanceBox.Value;
	if (mountainRiverChance < 0 || mountainRiverChance > 100)
		return Fail("Mountain river chance must be 0–100.");

	int maxRiversTM = (int)_maxRiversTMBox.Value;
	if (maxRiversTM < 0)
		return Fail("Max rivers from tall mountains cannot be negative.");

	// All good
	_warningLabel.Text = "";
	_generateButton.Disabled = false;
	return true;
}


private bool Fail(string msg)
{
	_warningLabel.Text = msg;
	_generateButton.Disabled = true;
	return false;
}


}
