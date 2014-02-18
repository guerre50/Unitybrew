using UnityEngine;
using System;
using System.Collections;

// http://forum.unity3d.com/threads/32760-Making-a-color-wheel-color-picker
public class ColorPicker : MonoBehaviour {
    public Texture2D colorPicker;
    public int ImageWidth = 100;
    public int ImageHeight = 100;

    void OnGUI(){
        if (GUI.RepeatButton(new Rect(10, 10, ImageWidth, ImageHeight), colorPicker)) {
            Vector2 pickpos = Event.current.mousePosition;
            int aaa = Convert.ToInt32(pickpos.x);
            int bbb = Convert.ToInt32(pickpos.y);
            Color col = colorPicker.GetPixel(aaa,41-bbb);
        }

    }

}