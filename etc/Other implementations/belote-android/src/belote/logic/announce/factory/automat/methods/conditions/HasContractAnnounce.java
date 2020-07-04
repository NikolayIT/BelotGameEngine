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
import belote.bean.announce.suit.AnnounceSuit;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceCondition;

/**
 * GameLastNoPassAnnounce class.
 * @author Dimitar Karamanov
 */
public final class HasContractAnnounce implements AnnounceCondition {

    /**
     * BelotGame instance.
     */
    private final Game game;

    /**
     * AnnounceSuit instance.
     */
    private final AnnounceSuit announceSuit;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public HasContractAnnounce(final Game game) {
        this(game, null);
    }

    /**
     * Constructor.
     * @param game BelotGame instance.
     * @param announceSuit instance.
     */
    public HasContractAnnounce(final Game game, final AnnounceSuit announceSuit) {
        this.game = game;
        this.announceSuit = announceSuit;
    }

    /**
     * The method which returns the result of condition.
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public boolean process(final Player player) {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        if (announce != null) {
            if (announceSuit != null) {
                return announce.getAnnounceSuit().equals(announceSuit);
            }
            return true;
        }
        return false;
    }
}
