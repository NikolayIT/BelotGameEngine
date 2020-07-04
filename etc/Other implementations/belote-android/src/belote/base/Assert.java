/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.base;

/**
 * Assert library. The library provides standard asserts methods.
 * @author Dimitar Karamanov
 */
public final class Assert {

    /**
     * Constructor
     */
    private Assert() {
        super();
    }

    /**
     * Asserts the provided object is not null.
     * @param object checked for null value. If the object is null a RuntimeException is thrown.
     * @param name object class type used in the error message.
     */
    public static void assertNotNull(final Object object, final Class<?> name) {
        if (object == null) {
            throw new RuntimeException("Expected not null parameter of type : " + name.getName());
        }
    }

    /**
     * Asserts the provided condition is true.
     * @param condition checked for validity. If the condition is false a RuntimeException is thrown.
     * @param message displayed on failure.
     */
    public static void assertTrue(final boolean condition, final String message) {
        if (!condition) {
            throw new RuntimeException("Assert true condition failed : " + message);
        }
    }

    /**
     * Asserts the provided array is not null and has a minimum length.
     * @param array which minimum length is checked.
     * @param length the minimum demand length size.
     */
    public static void assertArrayLenght(final Object[] array, final int length) {
        if (array == null) {
            throw new RuntimeException("Expected not null array to be provided ");
        }

        if (array.length < length) {
            throw new RuntimeException("Expected array with minimum lenght of " + length + " to be provided ");
        }
    }
}
