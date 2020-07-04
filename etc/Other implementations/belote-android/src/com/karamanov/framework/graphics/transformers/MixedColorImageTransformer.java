/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.framework.graphics.transformers;

import com.karamanov.framework.graphics.Color;

/**
 * MixedColorImageTransformer class.
 * @author Dimitar Karamanov
 */
public final class MixedColorImageTransformer extends BasicImageTransformer {

    /**
     * mix color red value.
     */
    private final int red;

    /**
     * mix color green value.
     */
    private final int green;

    /**
     * mix color blue value.
     */
    private final int blue;

    /**
     * Constructor.
     * @param color mix color.
     */
    public MixedColorImageTransformer(final Color color) {
        this.red = color.getRed();
        this.green = color.getGreen();
        this.blue = color.getBlue();
    }

    /**
     * Transform method.
     * @param pixel which be transformed.
     * @return int transformed pixel value.
     */
    protected int transformPixel(final int pixel) {
        final int RGB = pixel & 0x00FFFFFF;
        int r = Color.getRed(RGB);
        int g = Color.getGreen(RGB);
        int b = Color.getBlue(RGB);

        r = (r + red) >> 1;
        g = (g + green) >> 1;
        b = (b + blue) >> 1;

        return Color.getRGB(r, g, b);
    }
}