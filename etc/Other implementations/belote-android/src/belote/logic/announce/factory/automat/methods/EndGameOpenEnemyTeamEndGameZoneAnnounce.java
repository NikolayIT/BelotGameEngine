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
import belote.logic.announce.factory.automat.executors.EndGameOpenNormalAnnounce;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.OppositeTeamEndGameZone;
import belote.logic.announce.factory.automat.methods.conditions.TeamCanAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;

/**
 * EndGameOpenEnemyTeamEndGameZoneAnnounce class. Announce factory method which creates end game open announce.
 * @author Dimitar Karamanov
 */
public final class EndGameOpenEnemyTeamEndGameZoneAnnounce extends ConditionListMethod {

    /**
     * Normal announce factory helper.
     */
    private final AnnounceMethod engGameOpenNormalAnnounce;

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameOpenEnemyTeamEndGameZoneAnnounce(final Game game) {
        super(game);
        addAnnounceCondition(new MultipleAndCondition(new TeamCanAnnounce(game), new OppositeTeamEndGameZone(game)));
        engGameOpenNormalAnnounce = new EndGameOpenNormalAnnounce(game);
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(Player player) {
        return engGameOpenNormalAnnounce.getAnnounce(player);
    }
}
