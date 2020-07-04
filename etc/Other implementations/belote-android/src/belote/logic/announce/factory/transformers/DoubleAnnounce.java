/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.transformers;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;

/**
 * DoubleAnnounce class.
 * @author Dimitar Karamanov
 */
public final class DoubleAnnounce extends BaseAnnounceTransformer {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public DoubleAnnounce(Game game) {
        super(game);
    }

    /**
     * Transforms or not the provide announce to other depending conditions.
     * @param player which has to declare the announce.
     * @param announce which will be transformed.
     * @return the same or new announce.
     */
    public Announce transform(final Player player, final Announce announce) {
        final Announce lastNotPassAnnounce = game.getAnnounceList().getContractAnnounce();

        if (lastNotPassAnnounce != null) {
            if (!lastNotPassAnnounce.getPlayer().isSameTeam(announce.getPlayer()) && lastNotPassAnnounce.getAnnounceSuit().equals(announce.getAnnounceSuit())) {
                return Announce.createDoubleAnnounce(announce, player);
            }
        }
        return announce;
    }
}
