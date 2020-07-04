/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.validators;

import belote.bean.Player;
import belote.bean.pack.card.Card;

/**
 * Validatable interface used from all play strategies to validate the players move.
 * @author Dimitar Karamanov
 */
public interface Validatable {

    /**
     * Validates player card.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if the card is valid, false otherwise.
     */
    boolean validatePlayerCard(final Player player, final Card card);

    /**
     * Returns if the provided player has couple.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if has a couple false otherwise.
     */
    boolean hasPlayerCouple(final Player player, final Card card);
}
