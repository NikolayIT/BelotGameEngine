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

import belote.bean.Player;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceCondition;
import belote.logic.announce.factory.automat.methods.suitDeterminants.base.SuitDeterminant;

/**
 * PlayerSuit class. Returns true if the announce player has card from provided suit.
 * @author Dimitar Karamanov
 */
public final class HasSuit implements AnnounceCondition {

    /**
     * Suit determinant which dynamically by provided player determines the suit.
     */
    private final SuitDeterminant suitDeterminant;

    /**
     * Constructor.
     * @param suitDeterminant used to determine the suit.
     */
    public HasSuit(final SuitDeterminant suitDeterminant) {
        this.suitDeterminant = suitDeterminant;
    }

    /**
     * The method which returns the result of condition.
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public boolean process(final Player player) {
        return suitDeterminant.determineSuit(player) != null;
    }
}
