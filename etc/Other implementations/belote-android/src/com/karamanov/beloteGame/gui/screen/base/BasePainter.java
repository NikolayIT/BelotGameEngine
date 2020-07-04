/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.beloteGame.gui.screen.base;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Paint;
import android.graphics.drawable.BitmapDrawable;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.sequence.SequenceType;
import belote.bean.pack.square.Square;

import com.karamanov.beloteGame.R;
import com.karamanov.beloteGame.gui.graphics.PictureDecorator;
import com.karamanov.beloteGame.text.TextDecorator;
import com.karamanov.framework.graphics.Color;
import com.karamanov.framework.graphics.ImageUtil;
import com.karamanov.framework.graphics.Rectangle;

/**
 * BasePainter class.
 * @author Dimitar Karamanov
 */
public abstract class BasePainter {

    /**
     * Cashed card width.
     */
    protected final int cardWidth;

    /**
     * Cashed card height.
     */
    protected final int cardHeight;

    /**
     * Cashed card width.
     */
    protected final int cardBackWidth;

    /**
     * Cashed card height.
     */
    protected final int cardBackHeight;

    /**
     * Text decorator of game beans object (Suit, Rank, Announce ...)
     */
    protected final TextDecorator textDecorator;

    /**
     * Text decorator of game beans object (Suit, Rank, Announce ...)
     */
    protected final PictureDecorator pictureDecorator;

    protected final Context context;

    /**
     * mSmooth
     */
    private final Paint mSmooth;

    /**
     * Constructor.
     * @param width canvas width.
     * @param height canvas height.
     */
    protected BasePainter(Context context) {
        this.context = context;

        pictureDecorator = new PictureDecorator(context);
        cardWidth = pictureDecorator.getCardImage(Rank.Ace, Suit.Spade).getWidth();
        cardHeight = pictureDecorator.getCardImage(Rank.Ace, Suit.Spade).getHeight();
        cardBackWidth = pictureDecorator.getCardBackImageSmall().getWidth();
        cardBackHeight = pictureDecorator.getCardBackImageSmall().getHeight();
        textDecorator = new TextDecorator(context);

        mSmooth = new Paint(Paint.FILTER_BITMAP_FLAG);
        mSmooth.setAntiAlias(true);
        mSmooth.setDither(true);
    }

    public final int getCardWidth() {
        return cardWidth;
    }

    public final int getCardHeight() {
        return cardHeight;
    }

    public final int getDeskWidth() {
        return cardBackWidth;
    }

    public final int getDeskHeight() {
        return cardBackHeight;
    }

    public final Context getContext() {
        return context;
    }

    /**
     * Draws desk image.
     * @param g Graphics object.
     * @param x position.
     * @param y position.
     */
    protected void drawCardBackImage(final Canvas canvas, final int x, final int y) {
        Bitmap b = pictureDecorator.getCardBackImageSmall();
        canvas.drawBitmap(b, x, y, mSmooth);
    }

    protected void drawRotatedCardBackImage(final Canvas canvas, final int x, final int y) {
        Bitmap b = pictureDecorator.getCardBackImageSmall();
        canvas.save();
        try {
            canvas.rotate(90);
            canvas.drawBitmap(b, y, -x, mSmooth);
        } finally {
            canvas.restore();
        }
    }

    /**
     * Draw card to the canvas.
     * @param card which image is retrieve.
     * @param x - x coordinate.
     * @param y - y coordinate.
     * @param g - graphics object.
     */
    public final void drawCard(final Canvas canvas, final Card card, final int x, final int y) {
        Bitmap bitmap = pictureDecorator.getCardImage(card);
        if (bitmap != null) {
            canvas.drawBitmap(bitmap, x, y, mSmooth);
        }
    }

    /**
     * Draw card darkened to the canvas.
     * @param card which image is retrieve.
     * @param x - x coordinate.
     * @param y - y coordinate.
     * @param g - graphics object.
     */
    public final void drawDarkenedCard(final Canvas canvas, final Card card, final int x, final int y) {
        Bitmap picture = pictureDecorator.getCardImage(card);
        if (picture != null) {
            Bitmap b = ImageUtil.transformToDarkenedImage(picture);
            canvas.drawBitmap(b, x, y, mSmooth);
            b.recycle();
        }
    }

    /**
     * Draw card mixed with color to the canvas.
     * @param card which image is retrieve.
     * @param x - x coordinate.
     * @param y - y coordinate. param mixedColor used to transform the image.
     * @param g - graphics object.
     */
    public final void drawMixedColorCard(final Canvas canvas, final Card card, final int x, final int y, final Color mixedColor) {
        Bitmap picture = pictureDecorator.getCardImage(card);
        if (picture != null) {
            final Rectangle rec = new Rectangle(0, 0, picture.getWidth(), picture.getHeight());
            Bitmap b = ImageUtil.transformToMixedColorImage(picture, mixedColor, rec);
            canvas.drawBitmap(b, x, y, mSmooth);
            b.recycle();
        }
    }

    /**
     * Returns equal cards image.
     * @param equalCards which picture is retrieved.
     * @return Image equal cards image.
     */
    public final Bitmap getEqualCardsImage(final Square equalCards) {
        if (equalCards.getRank().equals(Rank.Jack)) {
            return ((BitmapDrawable) context.getResources().getDrawable(R.drawable.equal200)).getBitmap();
        }

        if (equalCards.getRank().equals(Rank.Nine)) {
            return ((BitmapDrawable) context.getResources().getDrawable(R.drawable.equal150)).getBitmap();
        }

        return ((BitmapDrawable) context.getResources().getDrawable(R.drawable.equal100)).getBitmap();
    }

    /**
     * Returns Image sequence's type image.
     * @param sequenceType which image is retrieved.
     * @return Image sequence's type image.
     */
    public final Bitmap getSequenceTypeImage(final SequenceType sequenceType) {
        if (sequenceType.equals(SequenceType.Quint)) {
            return ((BitmapDrawable) context.getResources().getDrawable(R.drawable.sequence100)).getBitmap();
        }

        if (sequenceType.equals(SequenceType.Quarte)) {
            return ((BitmapDrawable) context.getResources().getDrawable(R.drawable.sequence50)).getBitmap();
        }

        return ((BitmapDrawable) context.getResources().getDrawable(R.drawable.sequence20)).getBitmap();
    }

    /**
     * Returns couple image for provided suit.
     * @param suit instance.
     * @return Image instance.
     */
    public final Bitmap getCoupleImage(final Suit suit) {
        return pictureDecorator.getCoupleImage(suit);
    }

    /**
     * Returns suit's image.
     * @param suit which image is retrieved.
     * @return Image suit's image.
     */
    public final Bitmap getSuitImage(final Suit suit) {
        return pictureDecorator.getSuitImage(suit);
    }
}