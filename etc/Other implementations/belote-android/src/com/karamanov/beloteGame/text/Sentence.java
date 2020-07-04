/*
 * Copyright (c) i:FAO AG. All Rights Reserved.
 * Sentence.java
 * cytric mobile application.
 *
 * Created by mobile team Sep 30, 2010
 */
package com.karamanov.beloteGame.text;

/**
 * @author Dimitar Karamanov
 */
public final class Sentence {

    /**
     * Standard separator.
     */
    private static final String STANDARD_SEPARATOR = " ";

    /**
     * Text holder;
     */
    private final StringBuffer text = new StringBuffer();

    /**
     * Sentence separator.
     */
    private final String separator;

    /**
     * Constructor.
     */
    public Sentence() {
        this(STANDARD_SEPARATOR);
    }

    /**
     * Constructor.
     * @param separator - which is used between last and new one words.
     */
    public Sentence(String separator) {
        this.separator = separator;
    }

    /**
     * Adds word to sentence using default separator.
     * @param word - to be added.
     */
    public void addWord(String word) {
        addWord(word, separator);
    }

    /**
     * Adds a new line.
     */
    public void addNewLine() {
        String separator = System.getProperty("line.separator");
        text.append(separator);
    }

    /**
     * Adds word to sentence using the provided separator.
     * @param word - to be added.
     * @param separator - which is used between last and new one words.
     */
    public void addWord(String word, String separator) {
        if (word != null && !"".equals(word)) {
            if (text.length() != 0) {
                text.append(separator);
            }
            text.append(word);
        }
    }

    /**
     * Adds word to sentence in new line.
     * @param word - to be added.
     */
    public void addNewLineWord(String word) {
        if (word != null && !"".equals(word)) {
            if (text.length() != 0) {
                addNewLine();
            }
            text.append(word);
        }
    }

    /**
     * Returns sentence string representation.
     */
    public String toString() {
        return text.toString();
    }

    /**
     * Returns if the sentence is empty.
     * @return true if the sentence is empty, false otherwise.
     */
    public boolean isEmpty() {
        return text.length() == 0;
    }
}
