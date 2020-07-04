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
 * DarkenImageTransformer class.
 * @author Dimitar Karamanov
 */
public final class DarkenedImageTransformer extends BasicImageTransformer {

    /**
     * Transform method.
     * @param pixel which be transformed.
     * @return int transformed pixel value.
     */
    protected int transformPixel(final int pixel) {
        int RGB = pixel & 0x00FFFFFF;

        if (RGB == 0x00FFFFFF) {
            RGB = Color.getRGB(128);
        } else if (RGB > 0) {
            int r = Color.getRed(RGB);
            int g = Color.getGreen(RGB);
            int b = Color.getBlue(RGB);

            r = r >> 1;
            g = g >> 1;
            b = b >> 1;

            RGB = Color.getRGB(r, g, b);
        }
        return RGB;
    }
}
