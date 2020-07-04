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
import belote.logic.announce.factory.automat.methods.conditions.DealAttackPlayer;
import belote.logic.announce.factory.automat.methods.conditions.HasSuit;
import belote.logic.announce.factory.automat.methods.conditions.RankCount;
import belote.logic.announce.factory.automat.methods.conditions.SuitCount;
import belote.logic.announce.factory.automat.methods.conditions.TeamAttack;
import belote.logic.announce.factory.automat.methods.conditions.ThreePassOpenAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceCondition;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleOrCondition;
import belote.logic.announce.factory.automat.methods.conditions.base.NotCondition;
import belote.logic.announce.factory.automat.methods.suitDeterminants.AceTenKingSuit;

/**
 * RegGameNormalNotTrumpAnnounce class. Announce factory method which creates not trump announce.
 * @author Dimitar Karamanov
 */
public final class RegGameNormalNoTrumpAnnounce extends ConditionListMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public RegGameNormalNoTrumpAnnounce(final Game game) {
        super(game);

        AnnounceCondition teamAttackOrPass = new MultipleOrCondition(new TeamAttack(game), new ThreePassOpenAnnounce(game));
        AnnounceCondition teamDefence = new NotCondition(teamAttackOrPass);

        addAnnounceCondition(new MultipleAndCondition(new DealAttackPlayer(game), new SuitCount(new AceTenKingSuit(), 5)));
        // teamAttackOrPass
        addAnnounceCondition(new MultipleAndCondition(teamAttackOrPass, new RankCount(Rank.Ace, 3)));
        addAnnounceCondition(new MultipleAndCondition(teamAttackOrPass, new RankCount(Rank.Ace, 2), new RankCount(Rank.Ten, 1)));
        //addAnnounceCondition(new MultipleAndCondition(teamAttackOrPass, new RankCount(Rank.Ace, 1), new RankCount(Rank.Ten, 3)));
        // teamDefence
        addAnnounceCondition(new MultipleAndCondition(teamDefence, new RankCount(Rank.Ace, 3), new RankCount(Rank.Ten, 1)));
        addAnnounceCondition(new MultipleAndCondition(teamDefence, new RankCount(Rank.Ace, 2), new RankCount(Rank.Ten, 2)));
        addAnnounceCondition(new MultipleAndCondition(teamDefence, new RankCount(Rank.Ace, 2), new HasSuit(new AceTenKingSuit())));
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