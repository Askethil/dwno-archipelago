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
            if (!Application.isFocused) { StorySkip.Held = false; return; }
            if (Input.GetKeyDown(KeyCode.F9)) StorySkip.Toggle();
            StorySkip.Held = Input.GetKey(KeyCode.F8);
        }
        catch (Exception ex) { StorySkip.InputFailed(ex); }
    }
}
