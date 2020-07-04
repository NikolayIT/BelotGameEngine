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
 * DisabledImageTransformer class.
 * @author Dimitar Karamanov
 */
public final class DisabledImageTransformer extends BasicImageTransformer {

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

        final int gray = ((r + g + b) / 3 + 0xFF) / 2;
        r = gray;
        g = gray;
        b = gray;
        return Color.getRGB(r, g, b);
    }
}