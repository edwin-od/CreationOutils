using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AnimatorToolSaveScriptableObject : ScriptableObject
{
    [SerializeField] ToolData toolData;
    [SerializeField] Settings settings;

    public void Init()
    {
        toolData = new ToolData();
        settings = new Settings();
    }

    public void SaveToolData(float speed, bool looping) => toolData.Save(speed, looping);

    public ToolData Get() => toolData;

    [System.Serializable]
    public class ToolData
    {
        public float speed = 1;
        public bool looping = true;

        public void Save(float speed, bool looping)
        {
            this.speed = speed;
            this.looping = looping;
        }
    }

    public void SaveSettings(Color timelineBackgroundColor,
                             Color hierarchySelectedColorPause,
                             Color hierarchySelectedTextColorPause,
                             Color hierarchySelectedColorPlay,
                             Color hierarchySelectedTextColorPlay,
                             Color buttonPauseColor,
                             Color buttonPlayColor) 
        => settings.Save(timelineBackgroundColor,
                         hierarchySelectedColorPause,
                         hierarchySelectedTextColorPause,
                         hierarchySelectedColorPlay,
                         hierarchySelectedTextColorPlay,
                         buttonPauseColor,
                         buttonPlayColor);

    public Settings GetSettings() => settings;

    [System.Serializable]
    public class Settings
    {
        public Color timelineBackgroundColor = new Color(0.65f, 0.65f, 0.65f);
        public Color hierarchySelectedColorPause = new Color(0, 0.4811321f, 0.09050994f);
        public Color hierarchySelectedTextColorPause = Color.black;
        public Color hierarchySelectedColorPlay = new Color(0.4811321f, 0, 0);
        public Color hierarchySelectedTextColorPlay = Color.black;
        public Color buttonPauseColor = Color.green;
        public Color buttonPlayColor = Color.red;

        public void Save(Color timelineBackgroundColor,
                         Color hierarchySelectedColorPause,
                         Color hierarchySelectedTextColorPause,
                         Color hierarchySelectedColorPlay,
                         Color hierarchySelectedTextColorPlay,
                         Color buttonPauseColor,
                         Color buttonPlayColor)
        {
            this.timelineBackgroundColor = timelineBackgroundColor;
            this.hierarchySelectedColorPause = hierarchySelectedColorPause;
            this.hierarchySelectedTextColorPause = hierarchySelectedTextColorPause;
            this.hierarchySelectedColorPlay = hierarchySelectedColorPlay;
            this.hierarchySelectedTextColorPlay = hierarchySelectedTextColorPlay;
            this.buttonPauseColor = buttonPauseColor;
            this.buttonPlayColor = buttonPlayColor;

        }
    }
}
