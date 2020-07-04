/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.beloteGame.gui.screen.main;

import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.SharedPreferences.OnSharedPreferenceChangeListener;
import android.graphics.PointF;
import android.os.Bundle;
import android.preference.PreferenceManager;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.view.View.OnClickListener;
import android.view.ViewGroup.LayoutParams;
import android.widget.ImageButton;
import android.widget.RelativeLayout;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.R;
import com.karamanov.beloteGame.gui.screen.main.dealer.DealerFacade;
import com.karamanov.beloteGame.gui.screen.pref.BelotePreferencesActivity;
import com.karamanov.beloteGame.gui.screen.score.ScoreActivity;
import com.karamanov.beloteGame.gui.screen.tricks.TricksActivity;
import com.karamanov.framework.MessageActivity;
import com.karamanov.framework.message.Message;
import com.karamanov.framework.message.Messageable;

/**
 * BelotGameCanvas class.
 * 
 * @author Dimitar Karamanov
 */
public final class BeloteActivity extends MessageActivity implements OnSharedPreferenceChangeListener {

    public static final int NAV_PRESS = -1;
    public static final int NAV_LEFT = -2;
    public static final int NAV_RIGHT = -3;

    private static final int MENU_GAME_NEW_INDEX = 1;
    private static final int MENU_CARDS_INDEX = 2;
    private static final int MENU_SCORE_INDEX = 3;
    private static final int MENU_PREF_INDEX = 4;
    private static final int MENU_RESET_INDEX = 5;

    private DealerFacade dealer;
    private BeloteView beloteView;
    private RelativeLayout buttonsView;
    private RelativeLayout bodyView;

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        
        
        addMessageListener(Belote.MT_KEY_PRESSED, new KeyPressedListener());
        addMessageListener(Belote.MT_TOUCH_EVENT, new TouchListener());
        addMessageListener(Belote.MT_EXIT_EVENT, new ExitListener());
        addMessageListener(Belote.MT_PAINT_EVENT, new PaintListener());
        addMessageListener(Belote.MT_CLOSE_END_GAME, new CloseEndGameListener());
        
        SharedPreferences preferences = PreferenceManager.getDefaultSharedPreferences(this);
        String key = getString(R.string.prefBlackRedOrder);
        boolean blackRedOrder = preferences.getBoolean(key, Boolean.FALSE);
        Belote.getBeloteFacade(this).setBlackRedCardOrder(blackRedOrder);

        buttonsView = new RelativeLayout(this);
        buttonsView.setId(1);
        RelativeLayout.LayoutParams rlp = new RelativeLayout.LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.WRAP_CONTENT);
        rlp.addRule(RelativeLayout.ALIGN_PARENT_BOTTOM);
        buttonsView.setLayoutParams(rlp);
        
        ImageButton left = new ImageButton(this);
        left.setBackgroundResource(R.drawable.btn_left);
        rlp = new RelativeLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        rlp.addRule(RelativeLayout.ALIGN_PARENT_LEFT);
        rlp.addRule(RelativeLayout.CENTER_VERTICAL);
        left.setLayoutParams(rlp);
        left.setOnClickListener(new ButtonPressListener(Integer.valueOf(NAV_LEFT)));
        left.setSoundEffectsEnabled(false);
        buttonsView.addView(left);

        ImageButton play = new ImageButton(this);
        play.setBackgroundResource(R.drawable.btn_play);
        rlp = new RelativeLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        rlp.addRule(RelativeLayout.CENTER_IN_PARENT);
        play.setLayoutParams(rlp);
        play.setOnClickListener(new ButtonPressListener(Integer.valueOf(NAV_PRESS)));
        play.setSoundEffectsEnabled(false);
        buttonsView.addView(play);

        ImageButton right = new ImageButton(this);
        right.setBackgroundResource(R.drawable.btn_right);
        rlp = new RelativeLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        rlp.addRule(RelativeLayout.ALIGN_PARENT_RIGHT);
        rlp.addRule(RelativeLayout.CENTER_VERTICAL);
        right.setLayoutParams(rlp);
        right.setOnClickListener(new ButtonPressListener(Integer.valueOf(NAV_RIGHT)));
        right.setSoundEffectsEnabled(false);
        buttonsView.addView(right);
        
        boolean showBtns = preferences.getBoolean(getString(R.string.prefShowBtns), Boolean.TRUE);
        buttonsView.setVisibility(showBtns ? View.VISIBLE : View.GONE);

        beloteView = new BeloteView(this);
        
        dealer = new DealerFacade(this, beloteView, buttonsView);
        rlp = new RelativeLayout.LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.MATCH_PARENT);
        rlp.addRule(RelativeLayout.ABOVE, buttonsView.getId());
        beloteView.setLayoutParams(rlp);
        
        bodyView = new RelativeLayout(this);
        
        bodyView.addView(buttonsView);
        bodyView.addView(beloteView);
               
        setContentView(bodyView);

        preferences.registerOnSharedPreferenceChangeListener(this);
        
       
    }

    @Override
    protected void onDestroy() {
        SharedPreferences preferences = PreferenceManager.getDefaultSharedPreferences(this);
        preferences.unregisterOnSharedPreferenceChangeListener(this);
        
        super.onDestroy();
    }

    /**
     * Initialize the contents of the Activity's standard options menu. You should place your menu items in to menu.
     * 
     * @param menu - The options menu in which you place your items.
     */
    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        super.onCreateOptionsMenu(menu);
        int base = Menu.CATEGORY_SECONDARY;

        MenuItem newMenu = menu.add(base, base + MENU_GAME_NEW_INDEX, base + MENU_GAME_NEW_INDEX, getString(R.string.New));
        newMenu.setIcon(android.R.drawable.ic_menu_rotate);

        MenuItem scoreMenu = menu.add(base, base + MENU_SCORE_INDEX, base + MENU_SCORE_INDEX, getString(R.string.Score));
        scoreMenu.setIcon(android.R.drawable.ic_menu_info_details);

        MenuItem prefMenu = menu.add(base, base + MENU_PREF_INDEX, base + MENU_PREF_INDEX, getString(R.string.PREFERENCES));
        prefMenu.setIcon(android.R.drawable.ic_menu_manage);

        return true;
    }

    @Override
    public boolean onPrepareOptionsMenu(Menu menu) {
        super.onPrepareOptionsMenu(menu);

        int base = Menu.CATEGORY_SECONDARY;

        boolean showTricks = !Belote.getBeloteFacade(this).getGame().getTrickList().isEmpty();

        MenuItem historyMenu = menu.findItem(base + MENU_CARDS_INDEX);
        if (showTricks) {
            if (historyMenu == null) {
                historyMenu = menu.add(base, base + MENU_CARDS_INDEX, base + MENU_CARDS_INDEX, getString(R.string.PastTricks));
                historyMenu.setIcon(R.drawable.ic_menu_tricks);
            }
        } else {
            if (historyMenu != null) {
                menu.removeItem(base + MENU_CARDS_INDEX);
            }
        }

        MenuItem resetAnnounceMenu = menu.findItem(base + MENU_RESET_INDEX);
        if (Belote.getBeloteFacade(this).getGame().isAnnounceGameMode() && Belote.getBeloteFacade(this).getGame().getAnnounceList().getCount() > 0) {
            if (resetAnnounceMenu == null) {
                resetAnnounceMenu = menu.add(base, base + MENU_RESET_INDEX, base + MENU_RESET_INDEX, getString(R.string.ResetAnnounce));
                resetAnnounceMenu.setIcon(android.R.drawable.ic_menu_close_clear_cancel);
            }
        } else {
            if (resetAnnounceMenu != null) {
                menu.removeItem(base + MENU_RESET_INDEX);
            }
        }

        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        boolean result = super.onOptionsItemSelected(item);

        int base = Menu.CATEGORY_SECONDARY;

        if (item.getItemId() == base + MENU_GAME_NEW_INDEX) {
            AlertDialog.Builder myAlertDialog = new AlertDialog.Builder(this);
            myAlertDialog.setIcon(R.drawable.ic_launcher);
            myAlertDialog.setTitle(getString(R.string.Confirm));
            myAlertDialog.setMessage(getString(R.string.NewEraseQuestion));
            myAlertDialog.setPositiveButton(android.R.string.ok, new DialogInterface.OnClickListener() {
                public void onClick(DialogInterface dialog, int which) {
                    Belote.resetGame(BeloteActivity.this);
                    repaint();
                }
            });
            myAlertDialog.setNegativeButton(android.R.string.cancel, new DialogInterface.OnClickListener() {
                public void onClick(DialogInterface dialog, int which) {
                    //
                }
            });
            
            myAlertDialog.setCancelable(false);
            myAlertDialog.show();
        }

        if (item.getItemId() == base + MENU_CARDS_INDEX) {
            Intent intent = new Intent(this, TricksActivity.class);
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            intent.putExtra(TricksActivity.BELOTE, Belote.getBeloteFacade(this).getGame());
            startActivity(intent);
        }

        if (item.getItemId() == base + MENU_SCORE_INDEX) {
            Intent intent = new Intent(this, ScoreActivity.class);
            intent.putExtra(ScoreActivity.BELOTE, Belote.getBeloteFacade(this).getGame());
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            startActivity(intent);
        }

        if (item.getItemId() == base + MENU_PREF_INDEX) {
            Intent intent = new Intent(this, BelotePreferencesActivity.class);
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            startActivity(intent);
        }

        if (item.getItemId() == base + MENU_RESET_INDEX) {
            Belote.getBeloteFacade(this).getGame().getAnnounceList().clear();
            repaint();
        }

        return result;
    }

    private class KeyPressedListener implements Messageable {

        @Override
        public void performMessage(Message message) {
            if (message.getData() instanceof Integer) {
                Integer integer = (Integer) message.getData();
                dealer.checkKeyPressed(integer.intValue());
            }
        }
    }

    private class TouchListener implements Messageable {

        @Override
        public void performMessage(Message message) {
            if (message.getData() instanceof PointF) {
                PointF pointF = (PointF) message.getData();
                dealer.checkPointerPressed(pointF.x, pointF.y);
            }
        }
    }

    private class ExitListener implements Messageable {

        @Override
        public void performMessage(Message message) {
            dealer.onExit();
        }
    }

    private class PaintListener implements Messageable {

        @Override
        public void performMessage(Message message) {
            dealer.invalidateGame();
        }
    }
    
    private class CloseEndGameListener implements Messageable {

        @Override
        public void performMessage(Message message) {
            dealer.onCloseEndGame();
        }
    }
    
    @Override
    public void onBackPressed() {
        SharedPreferences preferences = PreferenceManager.getDefaultSharedPreferences(this);
        String key = getString(R.string.prefAlertOnQuit);
        boolean alertOnQuit = preferences.getBoolean(key, Boolean.TRUE);

        if (alertOnQuit) {
            AlertDialog.Builder myAlertDialog = new AlertDialog.Builder(this);
            myAlertDialog.setIcon(R.drawable.ic_launcher);
            myAlertDialog.setTitle(getString(R.string.Confirm));
            myAlertDialog.setMessage(getString(R.string.ExitQuestion));
            
            myAlertDialog.setPositiveButton(android.R.string.ok, new DialogInterface.OnClickListener() {
                public void onClick(DialogInterface dialog, int which) {
                    Message tMessage = new Message(Belote.MT_EXIT_EVENT);
                    triggerMessage(tMessage);
                }
            });
            
            myAlertDialog.setNegativeButton(android.R.string.cancel, new DialogInterface.OnClickListener() {
                public void onClick(DialogInterface dialog, int which) {
                    //
                }
            });
            
            myAlertDialog.setCancelable(false);
            myAlertDialog.show();
        } else {
            Message tMessage = new Message(Belote.MT_EXIT_EVENT);
            triggerMessage(tMessage);
        }
    }

    @Override
    public void onSharedPreferenceChanged(SharedPreferences sharedPreferences, String key) {
        if (key.equals(getString(R.string.prefShowBtns))) {
            boolean showBtns = sharedPreferences.getBoolean(key, Boolean.TRUE);
            buttonsView.setVisibility(showBtns ? View.VISIBLE : View.GONE);
        }

        if (key.equals(getString(R.string.prefBlackRedOrder))) {
            boolean blackRedOrder = sharedPreferences.getBoolean(key, Boolean.FALSE);
            Belote.getBeloteFacade(this).setBlackRedCardOrder(blackRedOrder);
            Belote.getBeloteFacade(this).arrangePlayersCards();
            repaint();
        }
    }

    public void onSurfaceChanged() {
        dealer.onSurfaceChanged();
    }

    private class ButtonPressListener implements OnClickListener {

        private final Integer i;

        public ButtonPressListener(Integer i) {
            this.i = i;
        }

        @Override
        public void onClick(View view) {
            Message tMessage = new Message(Belote.MT_KEY_PRESSED, i);
            triggerMessage(tMessage);
        }
    }
    
    @Override
    public void onResume() {
        super.onResume();
    }

    @Override
    public void onPause() {
        // Pause the AdView.
        super.onPause();
    }

}
