using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;

public partial class MainScreen : Control
{

#region Exports

	[ExportGroup("Sound Files")]
	[Export] private AudioStream sfx_strings;
	[Export] private AudioStream sfx_steel;
	[Export] private AudioStream sfx_retro;
	[Export] private AudioStream sfx_impact;

	[ExportGroup("Node References")]
	[Export] private NodePath path_animation_player;
	[Export] private NodePath path_spin_minutes;
	[Export] private NodePath path_spin_seconds;
	[Export] private NodePath path_flow_recent;
	[Export] private NodePath path_flow_presets;
	[Export] private NodePath path_check_play_sound;
	[Export] private NodePath path_option_sound;
	[Export] private NodePath path_audio_player;

#endregion

#region Consts
	private const string SAND_PERCENTAGE = "percent_visible";
	private const float SAND_VALUE_FULL = 1.0f;
	private const float SAND_VALUE_EMPTY = 0.0f;
	private const string ANIM_SHOW_PANEL = "ShowPanel";
	private const string DICT_KEY_RECENT = "RecentTimes";
	private const string DICT_KEY_PRESETS = "PresetTimes";
	private const string DICT_KEY_DO_PLAY_SOUND = "ShouldPlaySound";
	private const string DICT_KEY_SOUND_OPTION = "SoundFile";
	private const int MAX_RECENT_TIMES = 15;
	private const string DATA_FILE = "user://times.json";
	private const int SFX_STRINGS = 0;
	private const int SFX_STEEL = 1;
	private const int SFX_RETRO = 2;
	private const int SFX_IMPACT = 3;
#endregion

#region Refs

	private AnimationPlayer anim;
	private Tween tween = null;
	private SpinBox spinMinutes;
	private SpinBox spinSeconds;
	private FlowContainer flowRecent;
	private FlowContainer flowPresets;
	private CheckButton checkShouldPlaySong;
	private OptionButton optionSound;
	private AudioStreamPlayer audio;
#endregion

#region Data

	private List<int> TimePresets = new();
	private List<int> TimeRecents = new();

#endregion

#region GodotInterfacing
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Get(path_animation_player, out anim);
		Get(path_spin_minutes, out spinMinutes);
		Get(path_spin_seconds, out spinSeconds);
		Get(path_flow_presets, out flowPresets);
		Get(path_flow_recent, out flowRecent);
		Get(path_check_play_sound, out checkShouldPlaySong);
		Get(path_option_sound, out optionSound);
		Get(path_audio_player, out audio);
		LoadTimes();
		RefreshDisplay();
	}

	public override void _ExitTree()
	{
		SaveTimes();
	}

#endregion
#region Callbacks

	private void ToggleOptionsPanel(bool is_pressed){
		if (is_pressed) anim.Play(ANIM_SHOW_PANEL);
		else anim.PlayBackwards(ANIM_SHOW_PANEL);
	}

	private void TimeStart(){
		tween?.Stop();
		tween = GetTree().CreateTween();
		int time = GetTimeSeconds();
		tween.TweenMethod(new Callable(this, nameof(SetSandPercent)), SAND_VALUE_FULL, SAND_VALUE_EMPTY, time);
		tween.Finished += TimerCompleted;

		if (TimeRecents.Contains(time)) TimeRecents.Remove(time);
		TimeRecents.Add(time);
		RefreshDisplay();
	}
	private void TimePause(){
		tween?.Pause();
	}
	private void TimeResume(){
		tween?.Play();
	}

	private void TimerCompleted()
	{
		tween = null;

		if (!checkShouldPlaySong.ButtonPressed) return;
		
		AudioStream stream = null;
		switch (optionSound.Selected)
		{
			case SFX_STRINGS: stream = sfx_strings;
				break;
			case SFX_STEEL: stream = sfx_steel;
				break;
			case SFX_RETRO: stream = sfx_retro;
				break;
			case SFX_IMPACT: stream = sfx_impact;
				break;
		}
		audio.Stream = stream;
		audio.Play();
	}

	private void AddTimePreset()
	{
		int time = GetTimeSeconds();
		if (!TimePresets.Contains(time))
		{
			TimePresets.Add(time);
			RefreshDisplay();
		}
	}

	private void ApplyTimeSet(int minutes, int seconds)
	{
		spinMinutes.Value = minutes;
		spinSeconds.Value = seconds;
	}


#endregion

#region ButtonsDisplay

private void RefreshDisplay()
{
	// clear flow containers
	foreach(var c in flowRecent.GetChildren()) c.QueueFree();
	foreach(var c in flowPresets.GetChildren()) c.QueueFree();

	// Add Recents
	if (TimeRecents.Count > 0)
	{
		for(int i = TimeRecents.Count-1; i >= 0; i--)
		{
			int seconds = TimeRecents[i];
			int minutes = seconds / 60;
			seconds %= 60;

			var btn = new Button
			{
				Text = $"{minutes.ToString().PadZeros(2)}:{seconds.ToString().PadZeros(2)}"
			};
			btn.Pressed += () => ApplyTimeSet(minutes,seconds);
			flowRecent.AddChild(btn);
		}
	}
	// Add Recents
	foreach (var s in TimePresets)
	{
		int minutes = s / 60;
		int seconds = s % 60;

		var btn = new Button
		{
			Text = $"{minutes.ToString().PadZeros(2)}:{seconds.ToString().PadZeros(2)}"
		};
		btn.Pressed += () => ApplyTimeSet(minutes,seconds);
		flowPresets.AddChild(btn);
	}

}

#endregion

#region Utility

	private void SaveTimes()
	{
        using var file = FileAccess.Open(DATA_FILE, FileAccess.ModeFlags.Write);
        if (file == null) return;
        var dict = new Godot.Collections.Dictionary
        {
            [DICT_KEY_PRESETS] = TimePresets.ToArray(),
            [DICT_KEY_RECENT] = TimeRecents.ToArray(),
			[DICT_KEY_DO_PLAY_SOUND] = checkShouldPlaySong.ButtonPressed,
			[DICT_KEY_SOUND_OPTION] = optionSound.Selected
        };

        var json_text = Json.Stringify(dict);
        file.StoreString(json_text);
    }

	private void LoadTimes()
	{
		using var file = FileAccess.Open(DATA_FILE, FileAccess.ModeFlags.Read);
		if (file == null) return;

		var text = file.GetAsText();
		var dict = Json.ParseString(text).AsGodotDictionary();

		if (dict.ContainsKey(DICT_KEY_PRESETS))
		{
			TimePresets.Clear();
			TimePresets.AddRange(dict[DICT_KEY_PRESETS].AsInt32Array());
		}
		if (dict.ContainsKey(DICT_KEY_RECENT))
		{
			TimeRecents.Clear();
			TimeRecents.AddRange(dict[DICT_KEY_RECENT].AsInt32Array());
			if (TimeRecents.Count > 0) 
			{
				int mostRecent = TimeRecents.Last();
				int minutes = mostRecent / 60;
				int seconds = mostRecent % 60;
				spinMinutes.Value = minutes;
				spinSeconds.Value = seconds;
			}
		}
		if (dict.ContainsKey(DICT_KEY_DO_PLAY_SOUND))
		{
			checkShouldPlaySong.ButtonPressed = dict[DICT_KEY_DO_PLAY_SOUND].AsBool();
		}
		if (dict.ContainsKey(DICT_KEY_SOUND_OPTION))
		{
			optionSound.Selected = dict[DICT_KEY_SOUND_OPTION].AsInt32();
		}
	}

	private void ClearData()
	{
		TimePresets.Clear();
		TimeRecents.Clear();
		SaveTimes();
		RefreshDisplay();
	}

	private void SetSandPercent(float percentage)
	{
		RenderingServer.GlobalShaderParameterSet(SAND_PERCENTAGE, percentage);
	}

	private int GetTimeSeconds()
	{ // Is there a use case for multiple hours? The hourglass kinda loses the novelty if it's moving that slow
		return (int)((spinMinutes.Value * 60) + spinSeconds.Value);
	}

	private void Get<T>(NodePath path, out T node) where T : Node
	{
		node = GetNode<T>(path);
		if (node == null)
		{
			OS.Alert($"Failed to get node from path:\n\t{path}", "NodePath Failure");
			GD.PushError($"Failed to get node from path:\n\t{path}");
		}
	}


#endregion
}
