/*
 * Copyright (c) i:FAO AG. All Rights Reserved.
 * CytricSettingsActivity.java
 * cytric mobile application.
 *
 * Created by mobile team Feb 20, 2012
 */
package com.karamanov.beloteGame.gui.screen.pref;

import android.os.Bundle;
import android.preference.PreferenceActivity;

import com.karamanov.beloteGame.R;

public final class BelotePreferencesActivity extends PreferenceActivity {

    public BelotePreferencesActivity() {
        super();
    }

    /**
     * Called when the activity is first created.
     * @param saveInstanceState - Bundle contains the data it most recently supplied in onSaveInstanceState(Bundle).
     */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        addPreferencesFromResource(R.xml.preferences);
        setTitle(getString(R.string.PREFERENCES));
    }
}
