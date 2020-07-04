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
import belote.bean.announce.type.AnnounceType;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.OppositeTeamEndGameZone;

/**
 * EndGameNagDoubleAnnounce class. Announce factory method which creates double announce if the enemy team declared game and is in end game zone.
 * @author Dimitar Karamanov
 */
public final class EndGameNagDoubleAnnounce extends ConditionListMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameNagDoubleAnnounce(final Game game) {
        super(game);
        addAnnounceCondition(new OppositeTeamEndGameZone(game));
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(final Player player) {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        if (announce != null) {
            final boolean enemyAnnounce = !announce.getPlayer().isSameTeam(player);
            final boolean normalAnnounce = announce.getType().equals(AnnounceType.Normal);
            final boolean normalEnemyAnnounce = enemyAnnounce && normalAnnounce;
            final boolean enemyHasMorePoints = player.getTeam().getPoints().getAllPoints() < game.getOppositeTeam(player).getPoints().getAllPoints();

            if (enemyHasMorePoints && normalEnemyAnnounce && game.getAnnounceList().getCurrentPassAnnouncesCount() > 1) {
                return Announce.createDoubleAnnounce(announce, player);
            }
        }
        return null;
    }
}