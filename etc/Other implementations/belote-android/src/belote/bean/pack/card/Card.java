/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.card;

import belote.base.ComparableObject;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;

/**
 * Card class. Represents a pack card object.
 * @author Dimitar Karamanov
 */
public final class Card extends ComparableObject {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -8015898545101203826L;

    /**
     * Card's Suit {Spade,Heart,Diamond,Club}.
     */
    private final Suit suit;

    /**
     * Card's Rank {7,8,9,10,J,Q,K,A}.
     */
    private final Rank rank;

    /**
     * Standard hash.
     */
    private final int stHash;

    /**
     * All trump hash.
     */
    private final int atHash;

    /**
     * Not trump hash.
     */
    private final int ntHash;

    /**
     * ComparableMode.
     */
    private CardComparableMode comparableMode = CardComparableMode.Standard;

    /**
     * Acquire method
     */
    private String acquireMethod;

    /**
     * Constructor.
     * @param suit - Suit of the card.
     * @param rank - Rank of the card.
     */
    public Card(final Suit suit, final Rank rank) {
        this.suit = suit;
        this.rank = rank;

        stHash = Rank.getRankCount() * suit.getSuitOrder() + rank.getSTRankOrder();
        atHash = Rank.getRankCount() * suit.getSuitOrder() + rank.getATRankOrder();
        ntHash = Rank.getRankCount() * suit.getSuitOrder() + rank.getNTRankOrder();
    }

    /**
     * The method return card's points depending on comparable mode.
     * @return int card's points as a integer value.
     */
    public int getPoints() {
        return comparableMode.getPoints(rank);
    }

    /**
     * The method returns card's rank.
     * @return Rank the card's rank.
     */
    public Rank getRank() {
        return rank;
    }

    /**
     * The method returns card's suit.
     * @return Suit the card's suit.
     */
    public Suit getSuit() {
        return suit;
    }

    /**
     * Compares this card with the specified object(card) for order.
     * @param obj specified object (card).
     * @return int value which may be: = 0 if this card and the specified object(card) are equal > 0 if this card is bigger than the specified object(card) < 0
     *         if this card is less than the specified object(card)
     */
    public int compareTo(final Object obj) {
        final Card compCard = (Card) obj;
        return comparableMode.compareCardTo(this, compCard);
    }

    /**
     * Compares this card's rank with the specified card's rank for order using appropriate rank compare method depending on comparable mode.
     * @param card which rank is used to compare
     * @return int value which may be: = 0 if this card's rank and the specified card's rank are equal > 0 if this card's rank is bigger than the specified
     *         card's rank < 0 if this card's rank is less than the specified card's rank
     */
    public int compareRankTo(final Card card) {
        return compareRankTo(card.getRank());
    }

    /**
     * Compares this card's rank with the specified rank for order using appropriate rank compare method depending on comparable mode.
     * @param rank used to compare
     * @return int value which may be: = 0 if this card's rank and the specified rank are equal > 0 if this card's rank is bigger than the specified rank < 0 if
     *         this card's rank is less than the specified rank
     */
    public int compareRankTo(final Rank rank) {
        return comparableMode.compareRankTo(this.rank, rank);
    }

    /**
     * The method checks if this card and specified object (card) are equal.
     * @param obj specified object.
     * @return boolean true if this card is equal to specified object and false otherwise.
     */
    public boolean equals(final Object obj) {
        if (obj instanceof Card) {
            final Card card = (Card) obj;
            return suit.equals(card.suit) && rank.equals(card.rank);
        }
        return false;
    }

    /**
     * The method returns card's hash code.
     * @return int card's hash code value.
     */
    public int hashCode() {
        return getStHash();
    }

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with ofbuscating. Don't use this method to represent the object. When the project is compiled with ofbuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public String toString() {
        return rank.toString() + " " + suit.toString();
    }

    /**
     * Checks if this card rank is Rank.Queen or Rank.King.
     * @return boolean true if this card rank is Rank.Queen or Rank.King false otherwise.
     */
    public boolean isBeloteCard() {
        return rank.equals(Rank.King) || rank.equals(Rank.Queen);
    }

    /**
     * Checks if this card and specified card are from same suit and this card rank is bigger then specified card's rank.
     * @param card with which is checked
     * @return boolean true if this card and specified card suit are equal and this card rank is bigger than specified card's rank false otherwise.
     */
    public boolean isSameSuitBiggerCard(final Card card) {
        return card == null || (card.getSuit().equals(suit) && compareTo(card) > 0);
    }

    /**
     * Returns card's comparable mode.
     * @return CardComparableMode card's comparable mode.
     */
    public CardComparableMode getCompareMode() {
        return comparableMode;
    }

    /**
     * Sets card's comparable mode.
     * @param compareMode comparable mode.
     */
    public void setCompareMode(final CardComparableMode compareMode) {
        this.comparableMode = compareMode;
    }

    /**
     * Checks if the card is major card depending on card's comparable mode.
     * @return boolean true if this card is major false otherwise.
     */
    public boolean isMajorCard() {
        return comparableMode.isMajorRank(rank);
    }

    /**
     * Returns previous card from the same suit or this if this card is the less one from the suit depending on the card's comparable mode.
     * @return Card previous card or this.
     */
    public Card getSameSuitCardBefore() {
        if (rank.equals(comparableMode.getMinRank())) {
            return this;
        }
        return new Card(suit, comparableMode.getRankBefore(rank));
    }

    /**
     * Returns next card from the same suit or this if this card is the best one from the suit depending on the card's comparable mode.
     * @return Card next card or this.
     */
    public Card getSameSuitCardAfter() {
        if (rank.equals(comparableMode.getMaxRank())) {
            return this;
        }
        return new Card(suit, comparableMode.getRankAfter(rank));
    }

    /**
     * @return the stHash
     */
    public int getStHash() {
        return stHash;
    }

    /**
     * @return the atHash
     */
    public int getAtHash() {
        return atHash;
    }

    /**
     * @return the ntHash
     */
    public int getNtHash() {
        return ntHash;
    }

    /**
     * Sets acquire method name (Used for logic debugging).
     * @param acquireMethod acquire method
     */
    public void setCardAcquireMethod(final String acquireMethod) {
        this.acquireMethod = acquireMethod;
    }

    /**
     * Returns acquire method name (Used for logic debugging).
     * @return String the name of the acquire method
     */
    public String getCardAcquireMethod() {
        return acquireMethod;
    }
}
