//https://developer.mozilla.org/en-US/docs/Web/API/Vibration_API

mergeInto(LibraryManager.library, {
    _HasVibrator: function() {
        return (typeof navigator.vibrate === "function");
    },
    
    _Vibrate: function(duration) {
        navigator.vibrate(duration);
    }

    _VibratePattern: function(pattern) {
        navigator.vibrate(pattern);
    }
    
    _VibrateCancel: function() {
        navigator.vibrate(0);
    }
});
