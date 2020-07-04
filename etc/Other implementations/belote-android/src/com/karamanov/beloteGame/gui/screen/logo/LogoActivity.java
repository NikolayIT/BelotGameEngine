/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.beloteGame.gui.screen.logo;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.MotionEvent;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.gui.screen.main.BeloteActivity;

/**
 * LogoFrame class.
 * 
 * @author Dimitar Karamanov
 */
public final class LogoActivity extends Activity {

    private boolean send = false;

    /**
     * Constructor.
     * 
     * @param canvas parent container game canvas
     * @param rootComponent original root component
     */
    public LogoActivity() {
        super();
    }

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        LogoView view = new LogoView(this);
        setContentView(view);
    }

    @Override
    public boolean onKeyDown(int keyCode, KeyEvent event) {
        startBeloteActivity();
        return true;
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        startBeloteActivity();
        return true;
    }

    private void startBeloteActivity() {
        if (!send) {
            send = true;
            Belote.initBeloteFacade(this);
            Intent intent = new Intent(this, BeloteActivity.class);
            startActivity(intent);
        }
        finish();
    }
}