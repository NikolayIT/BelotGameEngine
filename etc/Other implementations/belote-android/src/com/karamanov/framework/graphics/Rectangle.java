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
 * Rectangle class.
 * @author Dimitar Karamanov
 */
public final class Rectangle {

    /**
     * Rectangle's x (left) position.
     */
    public int x;

    /**
     * Rectangle's y (top) position.
     */
    public int y;

    /**
     * Rectangle's width.
     */
    public int width;

    /**
     * Rectangle's height.
     */
    public int height;

    /**
     * Constructor.
     * @param x position.
     * @param y position.
     * @param width of the rectangle.
     * @param height of the rectangle.
     */
    public Rectangle(final int x, final int y, final int width, final int height) {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    /**
     * Validates size to be fixed into another rectangle size.
     * @param validator a rectangle which is the validate object.
     */
    public void validate(final Rectangle validator) {
        validateX(validator);
        validateY(validator);
        validateWidth(validator);
        validateHeight(validator);
    }

    /**
     * Returns true if the current rectangle includes the provided one, false otherwise.
     * @param rectangle which will be checked for include.
     * @return boolean true or false.
     */
    public boolean include(final Rectangle rectangle) {
        if (rectangle.x < x) {
            return false;
        }
        if (rectangle.x + rectangle.width > x + width) {
            return false;
        }
        if (rectangle.y < y) {
            return false;
        }
        if (rectangle.y + rectangle.height > y + height) {
            return false;
        }
        return true;
    }

    /**
     * Returns true if the current rectangle includes the provided one, false otherwise.
     * @param pX x coordinate of the object.
     * @param pY y coordinate of the object.
     * @return boolean true or false.
     */
    public boolean include(final int pX, final int pY) {
        if (pX < x) {
            return false;
        }
        if (pX > x + width) {
            return false;
        }
        if (pY < y) {
            return false;
        }
        if (pY > y + height) {
            return false;
        }
        return true;
    }

    /**
     * Returns union of current rectangle and provided one.
     * @param rectangle which will be united with current.
     * @return Rectangle union of both.
     */
    public Rectangle union(final Rectangle rectangle) {
        final int x1 = Math.min(rectangle.x, x);
        final int y1 = Math.min(rectangle.y, y);

        final int x2 = Math.max(rectangle.x + rectangle.width, x + width);
        final int y2 = Math.max(rectangle.y + height, y + height);

        return new Rectangle(x1, y1, x2 - x1, y2 - y1);
    }

    /**
     * Validates X to be fixed into another rectangle size.
     * @param validator a rectangle which is the validate object.
     */
    private void validateX(final Rectangle validator) {
        if (x < validator.x || x > validator.width) {
            x = validator.x;
        }
    }

    /**
     * Validates Y to be fixed into another rectangle size.
     * @param validator a rectangle which is the validate object.
     */
    private void validateY(final Rectangle validator) {
        if (y < validator.y || y > validator.height) {
            y = validator.y;
        }
    }

    /**
     * Validates Width to be fixed into another rectangle size.
     * @param validator a rectangle which is the validate object.
     */
    private void validateWidth(final Rectangle validator) {
        if (y + height > validator.y + validator.height) {
            height = validator.height + validator.y - y;
        }
    }

    /**
     * Validates Height to be fixed into another rectangle size.
     * @param validator a rectangle which is the validate object.
     */
    private void validateHeight(final Rectangle validator) {
        if (x + width > validator.x + validator.width) {
            width = validator.width + validator.x - x;
        }
    }
}
