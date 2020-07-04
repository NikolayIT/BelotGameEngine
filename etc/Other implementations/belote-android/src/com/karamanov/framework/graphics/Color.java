/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.framework.graphics;

/**
 * Color class.
 * @author Dimitar Karamanov
 */
public final class Color {

    /**
     * Color objects constants.
     */
    public final static Color clDarkGreen = new Color(0, 96, 0);

    public final static Color clGreen = new Color(0, 152, 0);

    public final static Color clLightGreen = new Color(0, 204, 0);

    public final static Color clPureGreen = new Color(0, 255, 0);

    public final static Color clPureYellow = new Color(128, 128, 0);

    public final static Color clDKCream = new Color(253, 241, 213);

    public final static Color clRed = new Color(255, 0, 0);

    public final static Color clLightRed = new Color(255, 64, 64);

    public final static Color clPink = new Color(255, 128, 128);

    public final static Color clBack = new Color(0, 192, 0);

    public final static Color clDarkRed = new Color(128, 0, 0);

    public final static Color clLightYellow = new Color(255, 255, 210);

    public final static Color clDarkGray = new Color(32);

    public final static Color clGray = new Color(128);

    public final static Color clMiddleGray = new Color(96);

    public final static Color clBlack = new Color(0);

    public final static Color clWhite = new Color(255);

    public final static Color clLightPink = new Color(255, 198, 198);

    public final static Color clDKBlue = new Color(30, 144, 255);

    public final static Color clDKTomato = new Color(255, 99, 71);

    public final static Color clDKGold = new Color(255, 140, 0);

    public final static Color clDKHay = new Color(255, 215, 0);

    public final static Color clDKSkyBlue = new Color(72, 209, 204);

    public final static Color clDKGreen = new Color(50, 205, 50);

    public final static Color clDKBorder = new Color(255, 239, 213);

    public final static Color clCream = new Color(255, 251, 240);

    public final static Color clNavy = new Color(0, 0, 128);

    public final static Color clActive = new Color(163, 184, 204);

    public final static Color clRadio = new Color(122, 138, 153);

    public final static Color clBlue = new Color(0, 0, 255);

    /**
     * RGB color value.
     */
    private int RGB;

    /**
     * Constructor.
     * @param gray segment
     */
    public Color(final int gray) {
        this(gray, gray, gray);
    }

    /**
     * Constructor.
     * @param red segment
     * @param green segment
     * @param blue segment
     */
    public Color(final int red, final int green, final int blue) {
        RGB = (red << 16) + (green << 8) + blue;
    }

    /**
     * Returns color's RGB value.
     * @return RGB value.
     */
    public int getRGB() {
        return 0xFF000000 | RGB;
    }

    /**
     * Returns color's Red value.
     * @return red value.
     */
    public int getRed() {
        return getRed(RGB);
    }

    /**
     * Returns color's Red value.
     * @param RGB of color.
     * @return red value.
     */
    public static int getRed(final int RGB) {
        return RGB >> 16;
    }

    /**
     * Returns color's Green value.
     * @return green value.
     */
    public int getGreen() {
        return getGreen(RGB);
    }

    /**
     * Returns color's Green value.
     * @param RGB of color.
     * @return green value.
     */
    public static int getGreen(final int RGB) {
        return (RGB & 0xFFFF) >> 8;
    }

    /**
     * Returns color's Blue value.
     * @return blue value.
     */
    public int getBlue() {
        return getBlue(RGB);
    }

    /**
     * Returns color's Blue value.
     * @param RGB of color.
     * @return blue value.
     */
    public static int getBlue(final int RGB) {
        return RGB & 0xFF;
    }

    /**
     * Returns color's low color.
     * @return Color low color.
     */
    public Color getLowColor() {
        final int LowR = getRed() >> 1;
        final int LowG = getGreen() >> 1;
        final int LowB = getBlue() >> 1;

        return new Color(LowR, LowG, LowB);
    }

    /**
     * Returns color's high color.
     * @return Color high color.
     */
    public Color getHighColor() {
        final int HighR = getHighColorSegment(getRed());
        final int HighG = getHighColorSegment(getGreen());
        final int HighB = getHighColorSegment(getBlue());

        return new Color(HighR, HighG, HighB);
    }

    /**
     * Transforms segment into high color segment.
     * @param segment which will be transformed.
     * @return high color segment.
     */
    private int getHighColorSegment(final int segment) {
        if (segment < 0x80) {
            return segment << 1;
        }
        return 0xFF;
    }

    /**
     * Returns RGB color by provided red, green and blue values.
     * @param red of the RGB
     * @param green of the RGB
     * @param blue of the RGB
     * @return RGB color.
     */
    public static int getRGB(final int gray) {
        return (gray << 16) + (gray << 8) + gray;
    }

    /**
     * Returns RGB color by provided red, green and blue values.
     * @param red of the RGB
     * @param green of the RGB
     * @param blue of the RGB
     * @return RGB color.
     */
    public static int getRGB(final int red, final int green, final int blue) {
        return (red << 16) + (green << 8) + blue;
    }
}
