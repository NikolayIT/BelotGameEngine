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
import belote.logic.announce.factory.automat.base.AnnounceMethod;
import belote.logic.announce.factory.automat.executors.EndGameNormalNormalAnnounce;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.TeamCanAnnounce;
import belote.logic.announce.factory.transformers.AnnounceTransformer;
import belote.logic.announce.factory.transformers.DoubleAnnounce;

/**
 * EndGameNormalEnemyTeamEndGameZoneAnnounce class. Announce factory method which creates an announce when the enemy teams has reached the end game zone.
 * @author Dimitar Karamanov
 */
public final class EndGameNormalEnemyTeamEndGameZoneAnnounce extends ConditionListMethod {

    /**
     * Normal announce factory helper.
     */
    private final AnnounceMethod endGameNormalNormalAnnounce;

    /**
     * Double announce factory helper.
     */
    private final AnnounceTransformer doubleAnnounce;

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameNormalEnemyTeamEndGameZoneAnnounce(final Game game) {
        super(game);
        addAnnounceCondition(new TeamCanAnnounce(game));
        endGameNormalNormalAnnounce = new EndGameNormalNormalAnnounce(game);
        doubleAnnounce = new DoubleAnnounce(game);
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(Player player) {
        Announce result = endGameNormalNormalAnnounce.getAnnounce(player);
        if (result != null) {
            result = doubleAnnounce.transform(player, result);
        }
        return result;
    }
}