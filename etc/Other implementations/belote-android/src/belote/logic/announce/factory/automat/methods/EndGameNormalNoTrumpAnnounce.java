/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.bean.pack.card.rank.Rank;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.RankCount;
import belote.logic.announce.factory.automat.methods.conditions.TeamAttack;
import belote.logic.announce.factory.automat.methods.conditions.TeamDefence;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;

/**
 * EndGameNormalNotTrumpAnnounce class. Announce factory method which creates normal not trump announce.
 * @author Dimitar Karamanov
 */
public final class EndGameNormalNoTrumpAnnounce extends ConditionListMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameNormalNoTrumpAnnounce(final Game game) {
        super(game);
        addAnnounceCondition(new RankCount(Rank.Ace, 4));
        addAnnounceCondition(new MultipleAndCondition(new TeamAttack(game), new RankCount(Rank.Ace, 3), new RankCount(Rank.Ten, 1)));
        addAnnounceCondition(new MultipleAndCondition(new TeamDefence(game), new RankCount(Rank.Ace, 3), new RankCount(Rank.Ten, 1)));
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(Player player) {
        return Announce.createNTNormalAnnounce(player);
    }

}