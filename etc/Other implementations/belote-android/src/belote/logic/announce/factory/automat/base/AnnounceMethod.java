/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.base;

import belote.bean.Player;
import belote.bean.announce.Announce;

/**
 * AnnounceMethod base class. Base class of all classes which return Announce instance.
 * @author Dimitar Karamanov
 */
public interface AnnounceMethod {

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Announce object instance or null.
     */
    Announce getAnnounce(final Player player);
}
