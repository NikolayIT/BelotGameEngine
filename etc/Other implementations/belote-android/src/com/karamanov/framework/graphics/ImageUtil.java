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

import android.graphics.Bitmap;

import com.karamanov.framework.graphics.transformers.DarkenedImageTransformer;
import com.karamanov.framework.graphics.transformers.DisabledImageTransformer;
import com.karamanov.framework.graphics.transformers.InvertedImageTransformer;
import com.karamanov.framework.graphics.transformers.MixedColorImageTransformer;

/**
 * ImageUtil class.
 * @author Dimitar Karamanov
 */
public final class ImageUtil {

    /**
     * Helper class which is used for drawing disabled style image.
     */
    private final static DisabledImageTransformer disabledImageTransformer = new DisabledImageTransformer();

    /**
     * Helper class which is used for drawing darken style image.
     */
    private final static DarkenedImageTransformer darkenedImageTransformer = new DarkenedImageTransformer();

    /**
     * Helper class which is used for drawing inverted style image.
     */
    private final static InvertedImageTransformer invertedImageTransformer = new InvertedImageTransformer();

    /**
     * Transforms provided image to disabled one.
     * @param image provided image.
     * @return Image transformed disabled image.
     */
    public static Bitmap transformToDisabledImage(final Bitmap image) {
        return disabledImageTransformer.transformImage(image);
    }

    /**
     * Transforms provided image to darkened one.
     * @param image provided image.
     * @return Image transformed darkened image.
     */
    public static Bitmap transformToDarkenedImage(final Bitmap image) {
        return darkenedImageTransformer.transformImage(image);
    }

    /**
     * Transforms provided image to inverted one.
     * @param image provided image.
     * @return Image transformed inverted image.
     */
    public static Bitmap transformToInvertedImage(final Bitmap image) {
        return invertedImageTransformer.transformImage(image);
    }

    /**
     * Transforms provided image to color mixed one.
     * @param image provided image.
     * @param mixedColor which will be used for mixed.
     * @param rec rectangle which will be mixed.
     * @return Image transformed color mixed image.
     */
    public static Bitmap transformToMixedColorImage(final Bitmap image, final Color mixedColor, final Rectangle rec) {
        return new MixedColorImageTransformer(mixedColor).transformImage(image, rec);
    }
}
