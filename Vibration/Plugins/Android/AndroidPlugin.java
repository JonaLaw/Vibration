// Thanks to neogeek for this code
// https://github.com/CandyCoded/HapticFeedback/blob/main/Assets/Plugins/CandyCoded.HapticFeedback/Plugins/Android/AndroidPlugin.java

package com.vibration.hapticfeedbacklibrary;

import android.app.Activity;
import android.content.Context;
import android.view.View;

public class AndroidPlugin {

    private Context context;
    private Activity activity;

    public AndroidPlugin(Context context) {
        this.context = context;
        this.activity = (Activity)context;
    }

    public boolean PerformHapticFeedback(int hapticFeedbackConstant, int hapticFeedbackFlag) {
        View view = activity.getWindow().getDecorView().findViewById(android.R.id.content);
        // FLAG_IGNORE_GLOBAL_SETTING is deprecated in API level 33
        // only privileged apps can ignore user settings for touch feedback
        return view.performHapticFeedback(hapticFeedbackConstant, hapticFeedbackFlag);
    }

    public boolean PerformHapticFeedback(int hapticFeedbackConstant) {
        View view = activity.getWindow().getDecorView().findViewById(android.R.id.content);
        return view.performHapticFeedback(hapticFeedbackConstant);
    }
}
