/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.beloteGame.gui.screen.main.announce;

import android.app.Dialog;
import android.graphics.Color;
import android.graphics.Typeface;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup.LayoutParams;
import android.view.Window;
import android.view.WindowManager;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.TextView;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.announce.type.AnnounceType;
import belote.logic.BeloteFacade;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.R;
import com.karamanov.beloteGame.text.TextDecorator;
import com.karamanov.framework.MessageActivity;

/**
 * AnnouncePanel class.
 * @author Dimitar Karamanov
 */
public class AnnounceDialog extends Dialog {

    private final LinearLayout vertical;

    /**
     * Pass button.
     */
    private final AnnounceButtonField jrbPass;

    /**
     * Clubs button.
     */
    private final AnnounceButtonField jrbClubs;

    /**
     * Diamond button.
     */
    private final AnnounceButtonField jrbDiamonds;

    /**
     * Hearts button.
     */
    private final AnnounceButtonField jrbHearts;

    /**
     * Spades button.
     */
    private final AnnounceButtonField jrbSpades;

    /**
     * Not trump button.
     */
    private final AnnounceButtonField jrbNotTrump;

    /**
     * All trump button.
     */
    private final AnnounceButtonField jrbAllTrump;

    /**
     * Double button.
     */
    private final AnnounceButtonField jrbDouble;

    /**
     * Redouble button.
     */
    private final AnnounceButtonField jrbRedouble;

    /**
     * All aces picture.
     */
    private final ImageView pAllAces;

    /**
     * All jacks picture.
     */
    private final ImageView pAllJacks;

    /**
     * Double picture.
     */
    private final TextView pDouble;

    /**
     * Redouble picture.
     */
    private final TextView pRedouble;

    /**
     * Club picture.
     */
    private final ImageView pClub;

    /**
     * Diamond picture.
     */
    private final ImageView pDiamond;

    /**
     * Heart picture.
     */
    private final ImageView pHeart;

    /**
     * Spade picture.
     */
    private final ImageView pSpade;

    /**
     * Announce label.
     */
    private final TextView announceLabel;

    /**
     * Belote game object.
     */
    private final BeloteFacade game;

    /**
     * Text decorator of game beans object (Suit, Rank, Announce ...)
     */
    private final TextDecorator decorator;

    private final MessageActivity activity;
    
    /**
     * Constructor.
     * @param game a BelotGame instance.
     * @param parent component.
     */
    public AnnounceDialog(MessageActivity context, final BeloteFacade game) {
        super(context);
        activity = context;
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_DIM_BEHIND);
        getWindow().setBackgroundDrawableResource(R.drawable.announce_dlg);

        int dip5 = Belote.fromPixelToDip(context, 5);
        int dip10 = Belote.fromPixelToDip(context, 10);

        vertical = new LinearLayout(context);
        vertical.setOrientation(LinearLayout.VERTICAL);
        vertical.setPadding(dip5, dip5, dip5, dip5);

        announceLabel = new TextView(context);
        announceLabel.setId(1);
        announceLabel.setTextColor(Color.rgb(255, 99, 71));
        announceLabel.setGravity(Gravity.CENTER_HORIZONTAL);
        vertical.addView(announceLabel);

        TableLayout tl = new TableLayout(context);

        this.game = game;
        decorator = new TextDecorator(context);

        MyFieldChangeListener mfl = new MyFieldChangeListener();

        TableRow row = new TableRow(context);
        // Left
        jrbClubs = new AnnounceButtonField(context, context.getString(R.string.ClubsAnnounce));
        jrbClubs.setOnClickListener(mfl);

        pClub = new ImageView(context);
        pClub.setImageResource(R.drawable.club);
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        lp.gravity = Gravity.CENTER;
        lp.rightMargin = dip5;
        pClub.setLayoutParams(lp);

        LinearLayout relative = new LinearLayout(context);

        relative.addView(pClub);
        relative.addView(jrbClubs);

        TableRow.LayoutParams trlp = new TableRow.LayoutParams();
        trlp.weight = 0.5f;
        relative.setLayoutParams(trlp);
        row.addView(relative);
        // Right
        jrbNotTrump = new AnnounceButtonField(context, context.getString(R.string.NotTrumpsAnnounce));
        jrbNotTrump.setOnClickListener(mfl);

        pAllAces = new ImageView(context);
        pAllAces.setImageResource(R.drawable.all_aces);

        trlp = new TableRow.LayoutParams();
        trlp.gravity = Gravity.CENTER;
        trlp.weight = 0.25f;
        trlp.leftMargin = dip10;
        pAllAces.setLayoutParams(trlp);
        row.addView(pAllAces);

        trlp = new TableRow.LayoutParams();
        trlp.weight = 0.25f;
        jrbNotTrump.setLayoutParams(trlp);
        row.addView(jrbNotTrump);

        tl.addView(row);

        // Third Row
        row = new TableRow(context);
        // Left
        jrbDiamonds = new AnnounceButtonField(context, context.getString(R.string.DiamondsAnnounce));
        jrbDiamonds.setOnClickListener(mfl);

        pDiamond = new ImageView(context);
        pDiamond.setImageResource(R.drawable.diamond);
        lp = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        lp.gravity = Gravity.CENTER;
        lp.rightMargin = dip5;
        pDiamond.setLayoutParams(lp);

        relative = new LinearLayout(context);
        relative.addView(pDiamond);
        relative.addView(jrbDiamonds);

        trlp = new TableRow.LayoutParams();
        trlp.weight = 0.5f;
        relative.setLayoutParams(trlp);
        row.addView(relative);

        // Right
        jrbAllTrump = new AnnounceButtonField(context, context.getString(R.string.AllTrumpsAnnounce));
        jrbAllTrump.setOnClickListener(mfl);

        pAllJacks = new ImageView(context);
        pAllJacks.setImageResource(R.drawable.all_jacks);
        
        trlp = new TableRow.LayoutParams();
        trlp.gravity = Gravity.CENTER;
        trlp.weight = 0.25f;
        trlp.leftMargin = dip10;
        pAllJacks.setLayoutParams(trlp);
        row.addView(pAllJacks);

        trlp = new TableRow.LayoutParams();
        trlp.weight = 0.25f;
        jrbAllTrump.setLayoutParams(trlp);
        row.addView(jrbAllTrump);

        tl.addView(row);

        // Fourth Row
        row = new TableRow(context);
        // Left
        jrbHearts = new AnnounceButtonField(context, context.getString(R.string.HeartsAnnounce));
        jrbHearts.setOnClickListener(mfl);

        pHeart = new ImageView(context);
        pHeart.setImageResource(R.drawable.heart);
        lp = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        lp.rightMargin = dip5;
        lp.gravity = Gravity.CENTER;
        pHeart.setLayoutParams(lp);

        relative = new LinearLayout(context);
        relative.addView(pHeart);
        relative.addView(jrbHearts);

        trlp = new TableRow.LayoutParams();
        trlp.weight = 0.5f;
        relative.setLayoutParams(trlp);
        row.addView(relative);

        // Right
        jrbDouble = new AnnounceButtonField(context, context.getString(R.string.DoubleAnnounce));
        jrbDouble.setOnClickListener(mfl);

        pDouble = new TextView(context);
        pDouble.setText("x 2");
        pDouble.setTypeface(Typeface.DEFAULT_BOLD);
        pDouble.setTextColor(Color.RED);
        
        trlp = new TableRow.LayoutParams();
        trlp.gravity = Gravity.CENTER;
        trlp.weight = 0.25f;
        trlp.leftMargin = dip10;
        pDouble.setLayoutParams(trlp);
        row.addView(pDouble);

        trlp = new TableRow.LayoutParams();
        trlp.weight = 0.25f;
        relative.setLayoutParams(trlp);
        row.addView(jrbDouble);

        tl.addView(row);

        // Sixth Row
        row = new TableRow(context);
        jrbSpades = new AnnounceButtonField(context, context.getString(R.string.SpadesAnnounce));
        jrbSpades.setOnClickListener(mfl);

        pSpade = new ImageView(context);
        pSpade.setImageResource(R.drawable.spade);
        lp = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
        lp.gravity = Gravity.CENTER;
        lp.rightMargin = dip5;
        pSpade.setLayoutParams(lp);

        relative = new LinearLayout(context);
        relative.addView(pSpade);
        relative.addView(jrbSpades);

        trlp = new TableRow.LayoutParams();
        trlp.weight = 0.5f;
        relative.setLayoutParams(trlp);
        row.addView(relative);

        // Right
        jrbRedouble = new AnnounceButtonField(context, context.getString(R.string.RedoubleAnnounce));
        jrbRedouble.setOnClickListener(mfl);

        pRedouble = new TextView(context);
        pRedouble.setText("x 4");
        pRedouble.setTypeface(Typeface.DEFAULT_BOLD);
        pRedouble.setTextColor(Color.RED);

        trlp = new TableRow.LayoutParams();
        trlp.gravity = Gravity.CENTER;
        trlp.weight = 0.25f;
        trlp.leftMargin = dip10;
        pRedouble.setLayoutParams(trlp);
        row.addView(pRedouble);

        trlp = new TableRow.LayoutParams();
        trlp.weight = 0.5f;
        jrbRedouble.setLayoutParams(trlp);
        row.addView(jrbRedouble);

        tl.addView(row);

        row = new TableRow(context);
        jrbPass = new AnnounceButtonField(context, context.getString(R.string.PassAnnounce));
        jrbPass.setOnClickListener(mfl);
        trlp = new TableRow.LayoutParams();
        trlp.span = 3;
        trlp.gravity = Gravity.CENTER_HORIZONTAL;

        relative = new LinearLayout(context);
        relative.addView(jrbPass);

        relative.setLayoutParams(trlp);
        row.addView(relative);

        tl.addView(row);

        vertical.addView(tl);

        setContentView(vertical);
        
        setCancelable(false);
        setCanceledOnTouchOutside(false);
    }

    public void onBackPressed() {
        // DN
    }

    /**
     * ButtonKeyPressedAdapter class. Implements KeyEventListener.
     */
    private class MyFieldChangeListener implements View.OnClickListener {

        @Override
        public void onClick(View view) {
            game.getGame().getAnnounceList().add(generatePlayerAnnounce(view));
            AnnounceDialog.this.dismiss();
        }
    }

    /**
     * Initialize method.
     */
    public void init() {
        initButtonsByAnnounces();
    }

    protected void onStop() {
        Belote.getBeloteFacade(activity).setPlayerIsAnnouncing(false);
        activity.repaint();
        Belote belote = (Belote) activity.getApplication();
        belote.getMessageProcessor().runMessaging();
    }

    /**
     * Buttons initialization depending current announce status.
     */
    private void initButtonsByAnnounces() {
        jrbDouble.setEnabled(false);
        jrbRedouble.setEnabled(false);
        pDouble.setEnabled(false);
        pRedouble.setEnabled(false);
        jrbDouble.setEnabled(false);
        jrbRedouble.setEnabled(false);

        jrbClubs.setEnabled(true);
        pClub.setEnabled(true);
        jrbDiamonds.setEnabled(true);
        pDiamond.setEnabled(true);
        jrbHearts.setEnabled(true);
        pHeart.setEnabled(true);
        jrbSpades.setEnabled(true);
        pSpade.setEnabled(true);
        jrbNotTrump.setEnabled(true);
        pAllAces.setEnabled(true);
        jrbAllTrump.setEnabled(true);
        pAllJacks.setEnabled(true);

        Announce last = game.getGame().getAnnounceList().getContractAnnounce();

        if (last != null) {
            Player lastAnnouncePlayer = last.getPlayer();
            Player announcePlayer = game.getNextAnnouncePlayer();

            boolean sameTeam = announcePlayer.getTeam() == lastAnnouncePlayer.getTeam();

            if (!sameTeam && last.getType().equals(AnnounceType.Normal)) {
                jrbDouble.setEnabled(true);
                pDouble.setEnabled(true);
            }

            if (!sameTeam && last.getType().equals(AnnounceType.Double)) {
                jrbRedouble.setEnabled(true);
                pRedouble.setEnabled(true);
            }

            if (last.getAnnounceSuit().compareTo(AnnounceSuit.Club) >= 0) {
                jrbClubs.setEnabled(false);
                pClub.setEnabled(false);
            }

            if (last.getAnnounceSuit().compareTo(AnnounceSuit.Diamond) >= 0) {
                jrbDiamonds.setEnabled(false);
                pDiamond.setEnabled(false);
            }

            if (last.getAnnounceSuit().compareTo(AnnounceSuit.Heart) >= 0) {
                jrbHearts.setEnabled(false);
                pHeart.setEnabled(false);
            }

            if (last.getAnnounceSuit().compareTo(AnnounceSuit.Spade) >= 0) {
                jrbSpades.setEnabled(false);
                pSpade.setEnabled(false);
            }

            if (last.getAnnounceSuit().compareTo(AnnounceSuit.NotTrump) >= 0) {
                jrbNotTrump.setEnabled(false);
                pAllAces.setEnabled(false);
            }

            if (last.getAnnounceSuit().compareTo(AnnounceSuit.AllTrump) >= 0) {
                jrbAllTrump.setEnabled(false);
                pAllJacks.setEnabled(false);
            }
        }

        if (last == null) {
            if (announceLabel.getVisibility() != View.GONE) {
                announceLabel.setVisibility(View.GONE);
            }
        } else {
            announceLabel.setText(decorator.getAnnounceTextEx(game.getGame().getAnnounceList()));
            if (announceLabel.getVisibility() != View.VISIBLE) {
                announceLabel.setVisibility(View.VISIBLE);
            }
        }
    }

    /**
     * Generates player announce by the receiver component.
     * @param receiver - the button which was pressed.
     * @return Announce instance.
     */
    private Announce generatePlayerAnnounce(final View receiver) {

        final Player player = game.getNextAnnouncePlayer();

        if (receiver == jrbPass) {
            return Announce.createPassAnnounce(player);
        }
        if (receiver == jrbClubs) {
            return Announce.createSuitNormalAnnounce(player, AnnounceSuit.Club);
        }
        if (receiver == jrbDiamonds) {
            return Announce.createSuitNormalAnnounce(player, AnnounceSuit.Diamond);
        }
        if (receiver == jrbHearts) {
            return Announce.createSuitNormalAnnounce(player, AnnounceSuit.Heart);
        }
        if (receiver == jrbSpades) {
            return Announce.createSuitNormalAnnounce(player, AnnounceSuit.Spade);
        }

        if (receiver == jrbNotTrump) {
            return Announce.createNTNormalAnnounce(player);
        }

        if (receiver == jrbAllTrump) {
            return Announce.createATNormalAnnounce(player);
        }

        if (receiver == jrbDouble) {
            Announce announce = game.getGame().getAnnounceList().getContractAnnounce();
            if (announce != null) {
                return Announce.createDoubleAnnounce(announce, player);
            }
        }

        if (receiver == jrbRedouble) {
            Announce announce = game.getGame().getAnnounceList().getContractAnnounce();
            if (announce != null) {
                return Announce.createRedoubleAnnounce(announce, player);
            }
        }

        return Announce.createPassAnnounce(player);
    }
}