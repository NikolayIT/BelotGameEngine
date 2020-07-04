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

import belote.bean.Player;
import belote.bean.announce.Announce;

/**
 * AnnounceTransformer interface.
 * @author Dimitar Karamanov
 */
public interface AnnounceTransformer {

    /**
     * Transforms or not the provide announce to other dependig conditions.
     * @param player which has to declare the announce.
     * @param announce which will be transformed.
     * @return the same or new announce.
     */
    Announce transform(Player player, Announce announce);
}
