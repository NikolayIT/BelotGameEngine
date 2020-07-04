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
import belote.logic.announce.factory.automat.methods.conditions.HasCard;
import belote.logic.announce.factory.automat.methods.conditions.RankCount;
import belote.logic.announce.factory.automat.methods.conditions.SuitCount;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;
import belote.logic.announce.factory.automat.methods.suitDeterminants.DominantSuit;
import belote.logic.announce.factory.automat.methods.suitDeterminants.base.SuitDeterminant;

/**
 * RegGameNagAllTrumpWhenFirstAndHasJackSuitAnnounce class. Announce factory method which creates nag all trump jack suit announce.
 * @author Dimitar Karamanov
 */
public final class RegGameNagAllTrumpDealAttackHasJackSuitAnnounce extends ConditionListMethod {

    private final SuitDeterminant suitDeterminant;

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public RegGameNagAllTrumpDealAttackHasJackSuitAnnounce(final Game game) {
        super(game);
        suitDeterminant = new DominantSuit();
        addAnnounceCondition(new MultipleAndCondition(new SuitCount(suitDeterminant, 5), new HasCard(Rank.Jack, suitDeterminant), new DealAttackPlayer(game)));
        addAnnounceCondition(new MultipleAndCondition(new SuitCount(suitDeterminant, 4), new HasCard(Rank.Jack, suitDeterminant), new DealAttackPlayer(game),
                new RankCount(Rank.Jack, 2)));
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(Player player) {
        return Announce.createATNormalAnnounce(player);
    }
}