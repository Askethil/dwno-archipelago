using System;
using UnityEngine;

namespace Dwno.Archipelago;

public sealed class StorySkipKeys : MonoBehaviour
{
    public StorySkipKeys(IntPtr pointer) : base(pointer) { }
    public void Update()
    {
        try
        {
            StorySkip.Focused = Application.isFocused;
            if (!StorySkip.Focused) { StorySkip.Held = false; return; }
            if (Input.GetKeyDown(KeyCode.F9)) StorySkip.Toggle();
            if (Input.GetKeyDown(KeyCode.F10)) StoryChoiceSkip.Toggle();
            StorySkip.Held = Input.GetKey(KeyCode.F8);
        }
        catch (Exception ex) { StorySkip.InputFailed(ex); }
    }
}
