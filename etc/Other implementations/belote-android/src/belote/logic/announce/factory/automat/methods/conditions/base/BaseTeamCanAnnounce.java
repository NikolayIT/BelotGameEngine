/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.conditions.base;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.sequence.Sequence;
import belote.bean.pack.sequence.SequenceIterator;
import belote.bean.pack.sequence.SequenceList;
import belote.bean.pack.sequence.SequenceType;
import belote.bean.pack.square.Square;
import belote.bean.pack.square.SquareIterator;
import belote.bean.pack.square.SquareList;

/**
 * BaseTeamCanAnnounce class.
 * @author Dimitar Karamanov
 */
public abstract class BaseTeamCanAnnounce implements AnnounceCondition {

    /**
     * Minimum points difference allowed to make end game announce.
     */
    private final int MinPointsToMakeAnnounce;

    /**
     * BelotGame instance.
     */
    private final Game game;

    /**
     * Constructor.
     * @param game BelotGame instance.
     * @param MinPointsToMakeAnnounce
     */
    public BaseTeamCanAnnounce(final Game game, final int MinPointsToMakeAnnounce) {
        this.game = game;
        this.MinPointsToMakeAnnounce = MinPointsToMakeAnnounce;
    }

    /**
     * The method which returns the result of condition.
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public final boolean process(final Player player) {
        final int oppositeTeamPoints = game.getOppositeTeam(player).getPoints().getAllPoints();
        final int playerTeamPoints = player.getTeam().getPoints().getAllPoints();
        final int extraPoints = getExtraPoints(player);

        return ((playerTeamPoints + extraPoints) - oppositeTeamPoints) >= MinPointsToMakeAnnounce;
    }

    /**
     * Returns player team extra points (Equals or/and Sequences).
     * @param player provided player.
     * @return int player team extra points (Equals or/and Sequences).
     */
    private int getExtraPoints(final Player player) {
        int result = 0;

        final SequenceList sequencesList = player.getCards().getSequencesList();
        final SequenceIterator sequencesIterator = sequencesList.iterator();

        if (sequencesIterator.hasNext()) {
            final Sequence sequence = sequencesIterator.next();
            if (sequence.getType().equals(SequenceType.Quint)) {
                result += sequence.getType().getSequencePoints();
            }
        }

        final SquareList squareList = player.getCards().getSquaresList();
        final SquareIterator squareIterator = squareList.iterator();

        if (squareIterator.hasNext()) {
            final Square square = squareIterator.next();
            if (square.getRank().equals(Rank.Jack) || square.getRank().equals(Rank.Nine)) {
                result += square.getPoints();
            }
        }
        return result;
    }
}
