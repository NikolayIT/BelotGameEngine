/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.conditions;

import belote.bean.Player;
import belote.bean.pack.card.rank.Rank;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceCondition;

/**
 * RankCount class. Returns true if the announce player has at minimum rank count.
 * @author Dimitar Karamanov
 */
public final class RankCount implements AnnounceCondition {

    /**
     * Rank which count is checking.
     */
    private final Rank rank;

    /**
     * Minimum count of rank needed.
     */
    private final int count;

    /**
     * Constructor.
     * @param rank which
     * @param count the minimum count of rank needed.
     */
    public RankCount(final Rank rank, final int count) {
        this.rank = rank;
        this.count = count;
    }

    /**
     * The method which returns the result of condition.
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public boolean process(final Player player) {
        final int rankCount = player.getCards().getRankCounts(rank);
        return rankCount >= count;
    }
}
