/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.Random;

import belote.bean.pack.card.Card;
import belote.bean.pack.card.CardComparableMode;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.rank.RankIterator;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitCountHashTable;
import belote.bean.pack.card.suit.SuitIterator;
import belote.bean.pack.sequence.SequenceList;
import belote.bean.pack.square.SquareList;

/**
 * Pack class. Represents a collection of cards objects.
 * @author Dimitar Karamanov
 */
public class Pack implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -5907720214058311509L;

    /**
     * Shuffle count.
     */
    private static final int SHUFFLE_COUNT = 1000;

    /**
     * Cashed full pack.
     */
    private final static Pack fullPack = new Pack(true);

    /**
     * Internal container collection.
     */
    private final ArrayList<Card> collection = new ArrayList<Card>();

    /**
     * Sequences list.
     */
    private final SequenceList sequencesList = new SequenceList();

    /**
     * Square list.
     */
    private final SquareList squaresList = new SquareList();

    /**
     * PackExtraAnnouncesManager announce manager.
     */
    private final PackExtraAnnouncesManager packExtraAnnouncesManager;
    
    /**
     * Random generator.
     */
    private final static Random random = new Random();

    /**
     * Default constructor
     */
    public Pack() {
        this(false);
    }

    /**
     * Constructor.
     * @param full indicates if the pack will be filled with cards or empty
     */
    public Pack(final boolean full) {
        packExtraAnnouncesManager = new PackExtraAnnouncesManager(this);
        if (full) {
            addAllCards();
        }
    }

    /**
     * Copy constructor.
     * @param pack a copy from pack.
     */
    public Pack(final Pack pack) {
        this(false);
        addAll(pack);
    }

    /**
     * Factory method which returns a new full pack.
     * @return Pack a full pack.
     */
    public final static Pack createFullPack() {
        fullPack.setCardsCompareMode(CardComparableMode.Standard);

        return new Pack(fullPack);
    }

    /**
     * Returns a suit pack.
     * @param suit specified suit
     * @return Pack a suit pack
     */
    public final Pack getSuitPack(final Suit suit) {
        final Pack result = new Pack();
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            if (card.getSuit().equals(suit)) {
                result.add(card);
            }
        }
        return result;
    }

    /**
     * Clear and copy all elements from another pack.
     * @param pack a copy from pack.
     */
    public final void copyFrom(final Pack pack) {
        collection.clear();
        addAll(pack);
    }

    /**
     * Add all elements from another pack.
     * @param pack a copy from pack.
     */
    public final void addAll(final Pack pack) {
        for (final PackIterator iterator = pack.iterator(); iterator.hasNext();) {
            Card card = iterator.next();
            add(card);
        }
    }

    /**
     * Returns true if the card is from couple, false otherwise.
     * @param card a checked card.
     * @return boolean true if the provided card is from a couple, false otherwise.
     */
    public final boolean hasCouple(final Card card) {
        if (card == null) {
            return false;
        }

        if (card.getRank().equals(Rank.Queen)) {
            return findCard(Rank.King, card.getSuit()) != null;
        }

        if (card.getRank().equals(Rank.King)) {
            return findCard(Rank.Queen, card.getSuit()) != null;
        }

        return false;
    }

    /**
     * Checks if the pack has a couple from provided suit.
     * @param suit checked suit.
     * @return boolean true if the pack has couple from the provided suit, false otherwise.
     */
    public final boolean hasCouple(final Suit suit) {
        return findCard(Rank.Queen, suit) != null && findCard(Rank.King, suit) != null;
    }

    /**
     * Returns the number of cards with specified suit.
     * @param suit specified suit.
     * @return int the number of cards with specified suit.
     */
    public final int getSuitCount(final Suit suit) {
        int result = 0;
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            if (card.getSuit().equals(suit)) {
                result++;
            }
        }
        return result;
    }

    /**
     * Returns true if has card(s) from the specified suit, otherwise false.
     * @param suit specified suit.
     * @return boolean true if has card(s) from the specified suit, otherwise false.
     */
    public final boolean hasSuitCard(final Suit suit) {
        return getSuitCount(suit) != 0;
    }

    /**
     * Returns the pack's dominant suit.
     * @return Suit pack's dominant suit.
     */
    public final Suit getDominantSuit() {
        Suit result = null;

        for (final SuitIterator iterator = Suit.iterator(); iterator.hasNext();) {
            final Suit suit = iterator.next();
            if (result == null || getSuitCount(suit) > getSuitCount(result)) {
                result = suit;
            }
        }

        return result;
    }

    /**
     * Clears the collection and adds all cards to it.
     */
    public final void addAllCards() {
        collection.clear();

        for (final SuitIterator suitIterator = Suit.iterator(); suitIterator.hasNext();) {
            final Suit suit = suitIterator.next();
            for (final RankIterator rankIterator = Rank.iterator(); rankIterator.hasNext();) {
                final Rank rank = rankIterator.next();
                collection.add(new Card(suit, rank));
            }
        }
    }

    /**
     * Shuffles the pack.
     */
    public final void shuffle() {
        for (int i = 0; i < SHUFFLE_COUNT; i++) {
            int randomInt = random.nextInt();
            final int index = Math.abs(randomInt % collection.size());
            if (index != collection.size() - 1) {
                final Card indexCard = collection.remove(index);
                collection.add(indexCard);
            }
        }
    }

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with obfuscating. Don't use this method to represent the object. When the project is compiled with obfuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public final String toString() {
        final StringBuffer sb = new StringBuffer();
        for (final PackIterator it = iterator(); it.hasNext();) {
            if (sb.length() != 0) {
                sb.append(" : ");
            }
            sb.append(it.next().toString());
        }
        return sb.toString();
    }

    /**
     * Prints the pack.
     */
    public final void printPack() {
        for (final PackIterator it = iterator(); it.hasNext();) {
            System.out.println(it.next());
        }
    }

    /**
     * Gets a card by index.
     * @param index of the card which to be returned.
     * @return Card a card instance.
     */
    public final Card getCard(final int index) {
        return (Card) collection.get(index);
    }

    /**
     * Returns pack's size.
     * @return int the size of the pack.
     */
    public final int getSize() {
        return collection.size();
    }

    /**
     * Returns true if the pack is empty false otherwise.
     * @return boolean true if the pack is empty false otherwise.
     */
    public final boolean isEmpty() {
        return collection.isEmpty();
    }

    /**
     * Removes a card by index from the pack.
     * @param index of the card which to be removed.
     * @return Card the removed card.
     */
    public final Card remove(final int index) {
        final Card result = (Card) collection.get(index);
        collection.remove(index);
        return result;
    }

    /**
     * Removes the provided card from the pack.
     * @param card which to be removed.
     * @return Card the removed card.
     */
    public final Card remove(final Card card) {
        final Card cardToRemove = findCard(card.getRank(), card.getSuit());
        if (cardToRemove != null) {
            collection.remove(cardToRemove);
            return cardToRemove;
        }
        return null;
    }

    /**
     * Adds the provided card to the pack.
     * @param card which to be added.
     */
    public final void add(final Card card) {
        collection.add(card);
    }

    /**
     * Removes all cards from the pack, which are stored in the provided pack.
     * @param pack which cards will be removed from the current one.
     * @return boolean true if all cards were removed, false otherwise.
     */
    public final boolean removeAll(final Pack pack) {
        boolean result = true;
        for (final PackIterator iterator = pack.iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            final Card cardToRemove = findCard(card.getRank(), card.getSuit());
            if (cardToRemove == null) {
                result = false;
            } else {
                collection.remove(cardToRemove);
            }
        }

        return result;
    }

    /**
     * Clear the pack (Removes all cards from the pack).
     */
    public final void clear() {
        collection.clear();
    }

    /**
     * Arranges the pack cards.
     */
    public final void arrange() {
        boolean swap = true;
        for (int i = 0, n = collection.size(); i < n - 1 && swap; i++) {
            swap = false;

            for (int j = 0, k = collection.size(); j < k - 1 - i; j++) {
                final Card cPrev = getCard(j);
                final Card cNext = getCard(j + 1);
                if (cPrev.compareTo(cNext) < 0) {
                    swap = true;
                    collection.remove(j);
                    collection.remove(j);
                    collection.add(j, cNext);
                    collection.add(j + 1, cPrev);
                }
            }
        }
    }

    /**
     * Arranges pack using NT order.
     */
    public final void arrangeNT() {
        setCardsCompareMode(CardComparableMode.NotTrump);
        arrange();
    }

    /**
     * Arranges pack using AT order.
     */
    public final void arrangeAT() {
        setCardsCompareMode(CardComparableMode.AllTrump);
        arrange();
    }

    /**
     * Arranges pack using CL order.
     * @param suit trump suit.
     */
    public final void arrangeCL(final Suit suit) {
        setCardsCompareMode(suit);
        arrange();
    }

    /**
     * Returns true if the pack has the next rank order card, otherwise false.
     * @param card which is checked.
     * @return boolean true if the pack has the next rank order card, otherwise false.
     */
    public final boolean hasNextFromSameSuit(final Card card) {
        final Card next = getNextFromSameSuit(card);
        return next != null && !next.equals(card);
    }

    /**
     * Returns the next rank order card.
     * @param card base one.
     * @return Card the next rank order card or null.
     */
    public final Card getNextFromSameSuit(final Card card) {

        if (card.getCompareMode().getMaxRank().equals(card.getRank())) {
            return card;
        } else {
            return findCard(card.getCompareMode().getRankAfter(card.getRank()), card.getSuit());
        }
    }

    /**
     * Returns true if the pack has the previous rank order card, otherwise false.
     * @param card base one.
     * @return boolean true if the pack has the previous rank order card, otherwise false.
     */
    public final boolean hasPrevFromSameSuit(final Card card) {
        final Card prev = getPrevFromSameSuit(card);
        return prev != null && !prev.equals(card);
    }

    /**
     * Returns the previous rank order card.
     * @param card which is checked.
     * @return Card the previous rank order card or null.
     */
    public final Card getPrevFromSameSuit(final Card card) {
        if (card.getCompareMode().getMinRank().equals(card.getRank())) {
            return card;
        } else {
            return findCard(card.getCompareMode().getRankBefore(card.getRank()), card.getSuit());
        }
    }

    /**
     * Returns specified by rank and suit card.
     * @param rank searching card's rank.
     * @param suit searching card's suit.
     * @return Card the searching card if the pack contains it, otherwise null.
     */
    public final Card findCard(final Rank rank, final Suit suit) {
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card currentCard = iterator.next();
            if (currentCard.getRank().equals(rank) && currentCard.getSuit().equals(suit)) {
                return currentCard;
            }
        }
        return null;
    }

    /**
     * Returns the card with the max rank from a specified suit.
     * @param suit the specified suit.
     * @return Card the max card or null.
     */
    public final Card findMaxSuitCard(final Suit suit) {
        Card result = null;
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            if (card.getSuit().equals(suit)) {
                if (result == null || result.compareTo(card) < 0) {
                    result = card;
                }
            }
        }
        return result;
    }

    /**
     * Returns the card with the max rank smaller than a specified one.
     * @param card the specified card.
     * @return Card the max card or null.
     */
    public final Card findMaxUnderCard(final Card card) {
        Card result = null;
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card currentCard = iterator.next();
            if (currentCard.getSuit().equals(card.getSuit()) && currentCard.compareTo(card) < 0) {
                if (result == null || result.compareTo(currentCard) < 0) {
                    result = currentCard;
                }
            }
        }
        return result;
    }

    /**
     * Returns the card with the minimum rank bigger than a specified one.
     * @param card the specified card.
     * @return Card the minimum card or null.
     */
    public final Card findMinAboveCard(final Card card) {
        Card result = null;
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card currentCard = iterator.next();
            if (currentCard.getSuit().equals(card.getSuit()) && currentCard.compareTo(card) > 0) {
                if (result == null || result.compareTo(currentCard) > 0) {
                    result = currentCard;
                }
            }
        }
        return result;
    }

    /**
     * Returns the card with the minimum rank from a specified suit.
     * @param suit the specified suit.
     * @return Card the minimum card or null.
     */
    public final Card findMinSuitCard(final Suit suit) {
        Card result = null;
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            if (card.getSuit().equals(suit)) {
                if (result == null || result.compareTo(card) > 0) {
                    result = card;
                }
            }
        }
        return result;
    }

    /**
     * Returns the card with the minimum rank from all cards.
     * @return Card the minimum card or null.
     */
    public final Card findMinAllCard() {
        return findMinAllCard(null);
    }

    /**
     * Returns the card with the minimum rank from all cards except the cards with the specified suit.
     * @param suit the specified suit.
     * @return Card the minimum card or null.
     */
    public final Card findMinAllCard(final Suit suit) {
        Card result = null;
        int resultCardPoints = 0;

        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card card = iterator.next();

            if (suit == null || !card.getSuit().equals(suit)) {
                int cardPoints = card.getPoints();

                if (result == null || cardPoints < resultCardPoints) {
                    result = card;
                    resultCardPoints = cardPoints;
                }
            }
        }
        return result;
    }

    /**
     * Returns the max sequence card after the specified one.
     * @param card specified card.
     * @return Card max sequence card.
     */
    public final Card getMaxSequenceCardAfter(final Card card) {
        Card result = card;
        while (hasNextFromSameSuit(result)) {
            result = getNextFromSameSuit(result);
        }
        return result;
    }

    /**
     * Returns the minimum sequence card before the specified one.
     * @param card specified card.
     * @return Card minimum sequence card.
     */
    public final Card getMinSequenceCardBefore(final Card card) {
        Card result = card;
        while (hasPrevFromSameSuit(result)) {
            result = getPrevFromSameSuit(result);
        }
        return result;
    }

    /**
     * Returns first not null card from cards pack.
     * @return Card player's first not null card.
     */
    public final Card getFirstNoNullCard() {
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            if (card != null) {
                return card;
            }
        }
        return null;
    }

    /**
     * Returns the number of the cards with the specified rank from the pack.
     * @param rank specified rank.
     * @return int the number of the cards with the specified rank from the pack.
     */
    public final int getRankCounts(final Rank rank) {
        int result = 0;

        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            if (iterator.next().getRank().equals(rank)) {
                result++;
            }
        }
        return result;
    }

    /**
     * Fills the extra announces.
     */
    public final void processExtraAnnounces() {
        packExtraAnnouncesManager.processExtraAnnounces();
    }

    /**
     * Returns the extra announces points.
     * @return int the extra announces points.
     */
    public final int getAnnouncePoints() {
        return packExtraAnnouncesManager.getAnnouncePoints();
    }

    /**
     * Sets cards compare mode.
     * @param compareMode the specified CardComparableMode.
     */
    private void setCardsCompareMode(final CardComparableMode compareMode) {
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            card.setCompareMode(compareMode);
        }
    }

    /**
     * Sets cards compare mode for suit game.
     * @param suit the game's suit.
     */
    private void setCardsCompareMode(final Suit suit) {
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card currentCard = iterator.next();
            if (currentCard.getSuit().equals(suit)) {
                currentCard.setCompareMode(CardComparableMode.AllTrump);
            } else {
                currentCard.setCompareMode(CardComparableMode.NotTrump);
            }
        }
    }

    /**
     * Returns a SuitCountHashTable holding suits and the count of pack cards with that suits.
     * @return a SuitCountHashTable holding suits and the count of pack cards with that suits, which may be empty.
     */
    public final SuitCountHashTable getSuitsDistribution() {
        final SuitCountHashTable result = new SuitCountHashTable();

        for (final SuitIterator iterator = Suit.iterator(); iterator.hasNext();) {
            final Suit suit = iterator.next();
            int count = this.getSuitCount(suit);

            if (count > 0) {
                result.put(suit, Integer.valueOf(count));
            }
        }

        return result;
    }

    /**
     * @return the sequences list
     */
    public SequenceList getSequencesList() {
        return sequencesList;
    }

    /**
     * @return the squares list
     */
    public SquareList getSquaresList() {
        return squaresList;
    }

    /**
     * Returns the pack iterator.
     * @return PackIterator the pack iterator.
     */
    public final PackIterator iterator() {
        return new PackIteratorImpl(collection.iterator());
    }
    
    public final void nullCardAcquireMethod() {
        for (final PackIterator iterator = iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            card.setCardAcquireMethod(null);
        }
    }

    /**
     * PackIteratorImpl class. Implements PackIterator interface.
     */
    private static class PackIteratorImpl implements PackIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<Card> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public PackIteratorImpl(final Iterator<Card> enumeration) {
            this.enumeration = enumeration;
        }

        /**
         * Returns true if the iteration has more elements.
         * @return boolean true if the iteration has more elements false otherwise.
         */
        public boolean hasNext() {
            return enumeration.hasNext();
        }

        /**
         * Returns the next element in the iteration.
         * @return Card the next element in the iteration.
         */
        public Card next() {
            return enumeration.next();
        }
    }
}
