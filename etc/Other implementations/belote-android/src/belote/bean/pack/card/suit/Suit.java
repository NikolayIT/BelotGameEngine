/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.card.suit;

import belote.base.ComparableObject;

/**
 * Suit class Represents Card's suit.
 * @author Dimitar Karamanov
 */
public abstract class Suit extends ComparableObject {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -7817406518667306126L;

    /**
     * Internal suit constant.
     */
    private final int suit;

    /**
     * Club suit constant.
     */
    public final static Suit Club;

    /**
     * Diamond suit constant.
     */
    public final static Suit Diamond;

    /**
     * Heart suit constant.
     */
    public final static Suit Heart;

    /**
     * Spade suit constant.
     */
    public final static Suit Spade;

    /**
     * Suit list used for iterations.
     */
    private final static SuitList suitList;

    /**
     * Static initialization.
     */
    static {
        Club = new Club();
        Diamond = new Diamond();
        Heart = new Heart();
        Spade = new Spade();

        suitList = new SuitList();

        suitList.add(Club);
        suitList.add(Diamond);
        suitList.add(Heart);
        suitList.add(Spade);
    }

    /**
     * Constructor.
     * @param suit suit constant.
     */
    protected Suit(final int suit) {
        this.suit = suit;
    }

    /**
     * Returns HTML suit's tag.
     * @return String HTML suit's tag.
     */
    public String getSuitImageTag() {
        return "<img src='../src/data/pictures/" + getClassShortName().toLowerCase() + ".png'>";
    }

    /**
     * Returns suit's color.
     * @return String suit's color.
     */
    public abstract String getSuitColor();

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with obfuscating. Don't use this method to represent the object. When the project is compiled with obfuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public String toString() {
        return getClassShortName();
    }

    /**
     * Returns suit's order.
     * @return int suit's order.
     */
    public int getSuitOrder() {
        return suit;
    }

    /**
     * Returns hash code.
     * @return int hash code.
     */
    public int hashCode() {
        int hash = 5;
        hash = 71 * hash + suit;
        return hash;
    }

    /**
     * The method checks if this Suit and specified object (Suit) are equal.
     * @param obj specified object.
     * @return boolean true if this Suit is equal to specified object and false otherwise.
     */
    public boolean equals(final Object obj) {
        if (obj instanceof Suit) {
            return suit == ((Suit) obj).suit;
        }
        return false;
    }

    /**
     * Returns suit iterator.
     * @return SuitIterator iterator.
     */
    public static SuitIterator iterator() {
        return suitList.iterator();
    }

    /**
     * Returns suit's count (4).
     * @return suit's count.
     */
    public static int getSuitCount() {
        return suitList.size();
    }

    /**
     * Compares this object with the specified object for order.
     * @param obj specified object.
     * @return int value which may be: = 0 if this object and the specified object are equal > 0 if this object is bigger than the specified object < 0 if this
     *         object is less than the specified object
     */
    public int compareTo(final Object obj) {
        final Suit compSuit = (Suit) obj;
        return suit - compSuit.suit;
    }
}
