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
import belote.bean.pack.square.SquareIterator;
import belote.bean.pack.square.SquareList;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceCondition;

/**
 * PlayerSquare class.
 * @author Dimitar Karamanov
 */
public final class HasSquare implements AnnounceCondition {

    public boolean process(final Player player) {
        final SquareList equalCardsList = player.getCards().getSquaresList();
        for (final SquareIterator iterator = equalCardsList.iterator(); iterator.hasNext();) {
            return true;
        }
        return false;
    }

}
