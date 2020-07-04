/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.suitDeterminants;

import belote.bean.Player;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.sequence.Sequence;
import belote.bean.pack.sequence.SequenceIterator;
import belote.bean.pack.sequence.SequenceList;
import belote.bean.pack.sequence.SequenceType;
import belote.logic.announce.factory.automat.methods.suitDeterminants.base.SuitDeterminant;

/**
 * Returns suit of which the player has sequence. (Only one is possible during game announce).
 * @author Dimitar Karamanov
 */
public final class SequenceSuit implements SuitDeterminant {

    /**
     * Sequence type (20, 50 or 100)
     */
    private final SequenceType sequenceType;

    /**
     * Maximum sequence rank.
     */
    private final Rank rank;

    /**
     * Constructor.
     */
    public SequenceSuit() {
        this(null, null);
    }

    /**
     * Constructor.
     * @param sequenceType looking for.
     */
    public SequenceSuit(final SequenceType sequenceType) {
        this(sequenceType, null);
    }

    /**
     * Constructor
     * @param sequenceType looking for.
     * @param rank Maximum sequence rank.
     */
    public SequenceSuit(final SequenceType sequenceType, final Rank rank) {
        this.sequenceType = sequenceType;
        this.rank = rank;
    }

    /**
     * Returns the determined suit.
     * @param player which has to declare the next announce.
     * @return Suit instance or null.
     */
    public Suit determineSuit(Player player) {
        final Sequence sequence = getSequence(player);
        if (sequence != null) {
            if ((sequenceType == null || sequence.getType().equals(sequenceType)) && (rank == null || rank.equals(sequence.getMaxCard().getRank()))) {
                return sequence.getMaxCard().getSuit();
            }
        }
        return null;
    }

    /**
     * Returns sequence suit.
     * @param player which has to declare next announce.
     * @return Suit instance or null.
     */
    public static Suit getSequenceSuit(Player player) {
        final Sequence sequence = getSequence(player);
        if (sequence != null) {
            return sequence.getMaxCard().getSuit();
        }
        return null;
    }

    /**
     * Returns first found sequence for provided player.
     * @param player which has to declare next announce.
     * @return Sequence instance or null.
     */
    private static Sequence getSequence(Player player) {
        final SequenceList sequencesList = player.getCards().getSequencesList();
        final SequenceIterator iterator = sequencesList.iterator();

        if (iterator.hasNext()) {
            return iterator.next();
        }
        return null;
    }
}
