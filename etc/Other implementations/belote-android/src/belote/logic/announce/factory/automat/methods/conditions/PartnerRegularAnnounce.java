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

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceCondition;

/**
 * PartnerRegularAnnounce class.
 * @author Dimitar Karamanov
 */
public final class PartnerRegularAnnounce implements AnnounceCondition {

    /**
     * BelotGame instance.
     */
    private final Game game;

    /**
     * Constructor.
     * @param game a BelotGame instance.
     */
    public PartnerRegularAnnounce(final Game game) {
        this.game = game;
    }

    /**
     * The method which returns the result of condition.
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public boolean process(final Player player) {
        final Announce partnerAnnounce = game.getAnnounceList().getContractAnnounce(player.getPartner());
        final Announce contractAnnounce = game.getAnnounceList().getContractAnnounce();

        // Partner made Announce
        return partnerAnnounce != null && contractAnnounce != null && partnerAnnounce.getAnnounceSuit().equals(contractAnnounce.getAnnounceSuit());
    }
}
