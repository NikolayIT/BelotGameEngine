/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.sequence;

import belote.base.ComparableObject;
import belote.bean.pack.card.Card;

/**
 * Sequence class. Represents card's sequence which is 3, 4 or 5 cards from same suit and consecutive ranks.
 * @author Dimitar Karamanov
 */
public final class Sequence extends ComparableObject {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -2337961220980036566L;

    /**
     * The max sequence's card (It marks the rank and the suit).
     */
    private final Card maxCard;

    /**
     * Sequence's type (3, 4 or 5 sequence cards).
     */
    private final SequenceType type;

    /**
     * Constructor.
     * @param maxCard max card of the sequence.
     * @param type sequence's type.
     */
    public Sequence(final Card maxCard, final SequenceType type) {
        this.maxCard = maxCard;
        this.type = type;
    }

    /**
     * Returns sequence's points.
     * @return int points.
     */
    public int getPoints() {
        return type.getSequencePoints();
    }

    /**
     * Returns sequence's type.
     * @return SequenceType sequence's type.
     */
    public SequenceType getType() {
        return type;
    }

    /**
     * Returns max card.
     * @return Card max card.
     */
    public Card getMaxCard() {
        return maxCard;
    }

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with ofbuscating. Don't use this method to represent the object. When the project is compiled with ofbuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public String toString() {
        return maxCard.toString() + "[" + type.getSequencePoints() + "]";
    }

    /**
     * Returns a anonymous string representation of the object.
     * @return a anonymous string representation of the object.
     */
    public String toAnonymousString() {
        return type.toString();
    }

    /**
     * Compares this sequence with specified sequence.
     * @param obj specified object.
     * @return int value which may be = 0 if this sequence and the specified sequence are equal > 0 if this sequence is bigger than the specified sequence < 0
     *         if this sequence is less than the specified sequence
     */
    public int compareTo(final Object obj) {
        final Sequence sequence = (Sequence) obj;
        final int typeCompare = type.compareTo(sequence.type);

        if (typeCompare == 0) {
            return maxCard.getRank().compareTo(sequence.maxCard.getRank());
        }

        return typeCompare;
    }

    /**
     * Returns hash code.
     * @return hash code.
     */
    public int hashCode() {
        return type.hashCode() * 100 + maxCard.hashCode();
    }

    /**
     * The method checks if this sequence and specified object (sequence) are equal.
     * @param obj specified object.
     * @return boolean true if this sequence is equal to specified object and false otherwise.
     */
    public boolean equals(final Object obj) {
        if (obj instanceof Sequence) {
            final Sequence sequence = (Sequence) obj;
            return type.equals(sequence.type) && maxCard.equals(sequence.maxCard);
        }
        return false;
    }
}
