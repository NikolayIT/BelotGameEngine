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

import android.graphics.Bitmap;

import com.karamanov.framework.graphics.Rectangle;

/**
 * BasicImageTransformer class.
 * @author Dimitar Karamanov
 */
public abstract class BasicImageTransformer {

    /**
     * Transform method.
     * @param pixel which be transformed.
     * @return int transformed pixel value.
     */
    protected abstract int transformPixel(final int pixel);

    /**
     * Transforms image template method.
     * @param image which will be transformed.
     * @return Image transformed image.
     */
    public final Bitmap transformImage(final Bitmap image) {
        return transformImage(image, null);
    }

    /**
     * Transforms image template method.
     * @param image which will be transformed.
     * @param rec the rectangle of the image which will be transformed.
     * @return Image transformed image.
     */
    public final Bitmap transformImage(final Bitmap image, final Rectangle rec) {
        final Rectangle rectangle;

        if (rec == null) {
            rectangle = new Rectangle(0, 0, image.getWidth(), image.getHeight());
        } else {
            rectangle = rec;
        }

        Bitmap result = Bitmap.createBitmap(image.getWidth(), image.getHeight(), Bitmap.Config.ARGB_8888);

        for (int row = rectangle.y, endRow = rectangle.y + rectangle.height; row < endRow; row++) {
            for (int col = rectangle.x, endCol = rectangle.x + rectangle.width; col < endCol; col++) {
                int pixel = image.getPixel(col, row);
                final int RGB = pixel & 0xFFFFFFFF;
                pixel = (pixel & 0xFF000000) | transformPixel(RGB);
                result.setPixel(col, row, pixel);
            }
        }

        return result;
    }
}
