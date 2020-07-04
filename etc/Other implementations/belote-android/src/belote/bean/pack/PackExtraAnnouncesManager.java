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

import belote.bean.pack.card.Card;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.rank.RankIterator;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;
import belote.bean.pack.sequence.Sequence;
import belote.bean.pack.sequence.SequenceIterator;
import belote.bean.pack.sequence.SequenceType;
import belote.bean.pack.square.Square;
import belote.bean.pack.square.SquareIterator;

/**
 * PackExtraAnnouncesManager class. A help facade for extracting equal cards and sequences from the pack.
 * @author Dimitar Karamanov
 */
public class PackExtraAnnouncesManager implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -7740837122383291209L;

    /**
     * Max sequence cards number.
     */
    private final static int MAX_SEQUENCE_CARD_COUNT = 5;

    /**
     * Cards count for sequence 20.
     */
    public final static int ST_020_COUNT = 3;

    /**
     * Cards count for sequence 50.
     */
    public final static int ST_050_COUNT = 4;

    /**
     * Cards count for sequence 100.
     */
    public final static int ST_100_COUNT = 5;

    /**
     * Internal pack object.
     */
    private final Pack pack;

    /**
     * Constructor.
     * @param pack the internal pack object.
     */
    public PackExtraAnnouncesManager(final Pack pack) {
        this.pack = pack;
    }

    /**
     * Fills the extra announces.
     */
    public final void processExtraAnnounces() {
        processSquares();
        processSequences();
    }

    /**
     * Returns the extra announces points.
     * @return int the extra announces points.
     */
    public final int getAnnouncePoints() {
        int result = 0;
        // Equals points
        for (SquareIterator it = pack.getSquaresList().iterator(); it.hasNext();) {
            result += it.next().getPoints();
        }
        // Sequences points
        for (SequenceIterator it = pack.getSequencesList().iterator(); it.hasNext();) {
            result += it.next().getPoints();
        }

        return result;
    }

    /**
     * Process equal cards.
     */
    private void processSquares() {
        pack.getSquaresList().clear();
        for (final RankIterator it = Rank.iterator(); it.hasNext();) {
            final Rank rank = it.next();
            if (rank.compareTo(Rank.Eight) > 0) {
                if (pack.getRankCounts(rank) == Suit.getSuitCount()) {
                    pack.getSquaresList().add(new Square(rank));
                }
            }
        }
    }

    /**
     * Process sequences.
     */
    private void processSequences() {
        pack.getSequencesList().clear();
        final Pack copyPack = new Pack(pack);

        // Remove equals card if we have
        for (final SquareIterator it = pack.getSquaresList().iterator(); it.hasNext();) {
            final Square equals = it.next();
            for (final SuitIterator sit = Suit.iterator(); sit.hasNext();) {
                final Suit suit = sit.next();
                final Card card = copyPack.findCard(equals.getRank(), suit);
                if (card != null) {
                    copyPack.remove(card);
                }
            }
        }

        for (final SuitIterator sit = Suit.iterator(); sit.hasNext();) {
            final Suit suit = sit.next();
            Card card = null;

            do {
                int count = 0;

                card = copyPack.findMaxSuitCard(suit);
                final Card maxCard = card;
                if (card != null) {
                    copyPack.remove(card);
                    count++;
                    while (copyPack.hasPrevFromSameSuit(card) && count < MAX_SEQUENCE_CARD_COUNT) {
                        count++;
                        card = copyPack.findCard(Rank.getSTRankBefore(card.getRank()), suit);
                        copyPack.remove(card);
                    }
                    createSequence(count, maxCard);
                }
            } while (card != null);
        }
    }

    /**
     * Factory method which creates sequence by specified count and max card.
     * @param count the specified count.
     * @param card the max sequence's card.
     */
    private void createSequence(final int count, final Card card) {
        switch (count) {
        case ST_020_COUNT:
            pack.getSequencesList().add(new Sequence(card, SequenceType.Tierce));
            break;
        case ST_050_COUNT:
            pack.getSequencesList().add(new Sequence(card, SequenceType.Quarte));
            break;
        case ST_100_COUNT:
            pack.getSequencesList().add(new Sequence(card, SequenceType.Quint));
            break;
        }
    }
}