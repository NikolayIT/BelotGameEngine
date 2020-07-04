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
 * SuitToAllTrumpAnnounce class. Transforms suit announce to all trump one when the last declared announce is higher than current.
 * @author Dimitar Karamanov
 */
public final class SuitToAllTrumpAnnounce extends BaseAnnounceTransformer {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public SuitToAllTrumpAnnounce(final Game game) {
        super(game);
    }

    /**
     * Transforms or not the provide announce to other depending conditions.
     * @param player which has to declare the announce.
     * @param announce which will be transformed.
     * @return the same or new announce.
     */
    public Announce transform(final Player player, final Announce announce) {
        Announce result = announce;

        if (announce != null) {
            final Player partner = player.getTeam().getPartner(player);
            final Announce partnerAnnounce = game.getAnnounceList().getContractAnnounce(partner);

            if (partnerAnnounce != null) {
                if (result.getAnnounceSuit().compareTo(partnerAnnounce.getAnnounceSuit()) < 0) {
                    result = Announce.createATNormalAnnounce(player);
                }
            }
        }
        return result;
    }
}
