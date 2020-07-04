/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.square;

import belote.base.ComparableObject;
import belote.bean.pack.card.rank.Rank;

/**
 * Square class. Represents 4 equal cards in the pack.
 * @author Dimitar Karamanov
 */
public final class Square extends ComparableObject {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -3899337028903726108L;
    
    /**
     * Square's rank.
     */
    private final Rank rank;

    /**
     * Constructor.
     * @param rank - Square's rank.
     */
    public Square(final Rank rank) {
        this.rank = rank;
    }

    /**
     * Returns Square's rank.
     * @return Rank square rank.
     */
    public Rank getRank() {
        return rank;
    }

    /**
     * Returns Square points.
     * @return int square points.
     */
    public int getPoints() {
        return rank.getSquarePoints();
    }

    /**
     * Compares this Square with the specified object(Square) for order.
     * @param obj - specified object (Square).
     * @return int value which may be: = 0 if this Square and the specified object(Square) are equal > 0 if this Square is bigger than the specified
     *         object(Square) < 0 if this EqSquareualCards is less than the specified object(Square)
     */
    public int compareTo(final Object obj) {
        final Square square = (Square) obj;
        return rank.compareToAT(square.rank);
    }

    /**
     * Returns hash code.
     * @return hash code.
     */
    public int hashCode() {
        return rank.hashCode();
    }

    /**
     * The method checks if this Square and specified object (Square) are equal.
     * @param obj - specified object.
     * @return boolean true if this Square is equal to specified object and false otherwise.
     */
    public boolean equals(final Object obj) {
        if (obj instanceof Square) {
            final Square square = (Square) obj;
            return rank.equals(square.rank);
        }
        return false;
    }

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with obfuscating. Don't use this method to represent the object. When the project is compiled with obfuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public String toString() {
        return String.valueOf(rank.getSquarePoints()) + "(4x" + rank.getRankSign() + ")";
    }
}
