using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public class WaitForSeconds {
    private float duration;
    private float timer = 0;

    public WaitForSeconds(float t) {
        duration = t;
    }

    public bool Tick(float t) {
        timer += t;
        if (timer >= duration) {
            timer -= duration;
            return true;
        }

        return false;
    }
}
