/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.beloteGame.gui.graphics;

import java.util.Hashtable;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.NinePatchDrawable;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;

import com.karamanov.beloteGame.R;

/**
 * PictureDecorator class.
 * @author Dimitar Karamanov
 */
public final class PictureDecorator {

    /**
     * Couples hashtable.
     */
    private final Hashtable<Suit, Integer> couples = new Hashtable<Suit, Integer>();

    /**
     * Suits container.
     */
    private final Hashtable<Suit, Integer> suits = new Hashtable<Suit, Integer>();

    /**
     * Ranks container.
     */
    private final Hashtable<Rank, Hashtable<Suit, Integer>> ranks = new Hashtable<Rank, Hashtable<Suit, Integer>>();

    /**
     * Context.
     */
    private final Context context;

    /**
     * Constructor.
     */
    public PictureDecorator(Context context) {
        this.context = context;

        suits.put(Suit.Club, R.drawable.club);
        suits.put(Suit.Diamond, R.drawable.diamond);
        suits.put(Suit.Heart, R.drawable.heart);
        suits.put(Suit.Spade, R.drawable.spade);

        Hashtable<Suit, Integer> aces = new Hashtable<Suit, Integer>();
        aces.put(Suit.Club, R.drawable.aceclub);
        aces.put(Suit.Diamond, R.drawable.acediamond);
        aces.put(Suit.Heart, R.drawable.aceheart);
        aces.put(Suit.Spade, R.drawable.acespade);
        ranks.put(Rank.Ace, aces);

        Hashtable<Suit, Integer> kings = new Hashtable<Suit, Integer>();
        kings.put(Suit.Club, R.drawable.kingclub);
        kings.put(Suit.Diamond, R.drawable.kingdiamond);
        kings.put(Suit.Heart, R.drawable.kingheart);
        kings.put(Suit.Spade, R.drawable.kingspade);
        ranks.put(Rank.King, kings);

        Hashtable<Suit, Integer> queens = new Hashtable<Suit, Integer>();
        queens.put(Suit.Club, R.drawable.queenclub);
        queens.put(Suit.Diamond, R.drawable.queendiamond);
        queens.put(Suit.Heart, R.drawable.queenheart);
        queens.put(Suit.Spade, R.drawable.queenspade);
        ranks.put(Rank.Queen, queens);

        Hashtable<Suit, Integer> jacks = new Hashtable<Suit, Integer>();
        jacks.put(Suit.Club, R.drawable.jackclub);
        jacks.put(Suit.Diamond, R.drawable.jackdiamond);
        jacks.put(Suit.Heart, R.drawable.jackheart);
        jacks.put(Suit.Spade, R.drawable.jackspade);
        ranks.put(Rank.Jack, jacks);

        Hashtable<Suit, Integer> tens = new Hashtable<Suit, Integer>();
        tens.put(Suit.Club, R.drawable.tenclub);
        tens.put(Suit.Diamond, R.drawable.tendiamond);
        tens.put(Suit.Heart, R.drawable.tenheart);
        tens.put(Suit.Spade, R.drawable.tenspade);
        ranks.put(Rank.Ten, tens);

        Hashtable<Suit, Integer> nines = new Hashtable<Suit, Integer>();
        nines.put(Suit.Club, R.drawable.nineclub);
        nines.put(Suit.Diamond, R.drawable.ninediamond);
        nines.put(Suit.Heart, R.drawable.nineheart);
        nines.put(Suit.Spade, R.drawable.ninespade);
        ranks.put(Rank.Nine, nines);

        Hashtable<Suit, Integer> eights = new Hashtable<Suit, Integer>();
        eights.put(Suit.Club, R.drawable.eightclub);
        eights.put(Suit.Diamond, R.drawable.eightdiamond);
        eights.put(Suit.Heart, R.drawable.eightheart);
        eights.put(Suit.Spade, R.drawable.eightspade);
        ranks.put(Rank.Eight, eights);

        Hashtable<Suit, Integer> sevens = new Hashtable<Suit, Integer>();
        sevens.put(Suit.Club, R.drawable.sevenclub);
        sevens.put(Suit.Diamond, R.drawable.sevendiamond);
        sevens.put(Suit.Heart, R.drawable.sevenheart);
        sevens.put(Suit.Spade, R.drawable.sevenspade);
        ranks.put(Rank.Seven, sevens);

        couples.put(Suit.Club, R.drawable.belotclub);
        couples.put(Suit.Diamond, R.drawable.belotdiamond);
        couples.put(Suit.Heart, R.drawable.belotheart);
        couples.put(Suit.Spade, R.drawable.belotspade);
    }

    /**
     * The method return associated image to the card.
     * @param card which image is retrieve.
     * @return Image the card's image.
     */
    public Bitmap getCardImage(final Card card) {
        if (card != null) {
            Hashtable<Suit, Integer> hashtable = ranks.get(card.getRank());
            return ((BitmapDrawable) context.getResources().getDrawable(hashtable.get(card.getSuit()).intValue())).getBitmap();
        }
        return null;
    }

    /**
     * The method return associated image to the card.
     * @param card which image is retrieve.
     * @return Image the card's image.
     */
    public Bitmap getCardImage(final Rank rank, final Suit suit) {
        Hashtable<Suit, Integer> hashtable = ranks.get(rank);
        return ((BitmapDrawable) context.getResources().getDrawable(hashtable.get(suit).intValue())).getBitmap();
    }

    /**
     * Returns suit's image.
     * @param suit which image is retrieved.
     * @return Image suit's image.
     */
    public Bitmap getSuitImage(final Suit suit) {
        return ((BitmapDrawable) context.getResources().getDrawable(suits.get(suit).intValue())).getBitmap();
    }

    /**
     * Returns couples image from provided suit.
     * @param suit provided suit.
     * @return Image couples image.
     */
    public Bitmap getCoupleImage(final Suit suit) {
        return ((BitmapDrawable) context.getResources().getDrawable(couples.get(suit).intValue())).getBitmap();
    }

    /**
     * Returns the image of card desk.
     * @return desk image.
     */
    public Bitmap getCardBackImageSmall() {
        return ((BitmapDrawable) context.getResources().getDrawable(R.drawable.card_back_small)).getBitmap();
    }

    public NinePatchDrawable getBubbleLeft() {
        return (NinePatchDrawable) context.getResources().getDrawable(R.drawable.bubble_left);
    }

    public NinePatchDrawable getBubbleRight() {
        return (NinePatchDrawable) context.getResources().getDrawable(R.drawable.bubble_right);
    }

    public GradientDrawable getMainBKG() {
        return ((GradientDrawable) context.getResources().getDrawable(R.drawable.main_bkg));
    }
}
