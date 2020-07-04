/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.base;

import belote.bean.Player;
import belote.bean.pack.card.Card;

/**
 * PlayCardMethod interface.
 * @author Dimitar Karamanov
 */
public interface PlayCardMethod {

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    Card getPlayerCard(final Player player);
}
