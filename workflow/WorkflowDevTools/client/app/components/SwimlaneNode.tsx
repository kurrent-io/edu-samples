/** Subflow parent node  to render a swimlane */
import { Box, Heading, HStack } from "@chakra-ui/react"
import styles from "./SwimlaneNode.module.css"
import { styleConstants } from "./styleConstants"

interface SwimlaneNodeProps {
  data: {
    swimlaneName: string
  }
}

const SwimlaneNode = ({ data: { swimlaneName } }: SwimlaneNodeProps) => {
  return (
    <HStack
      className={styles.swimlaneNode}
      style={{ height: styleConstants.swimlane.height }}
    >
      <Box
        width={styleConstants.swimlane.swimlaneNameContainerWidth}
        className={styles.swimlaneName}
      >
        <Heading>{swimlaneName}</Heading>
      </Box>
      <Box className={styles.swimlaneCardsContainer} />
    </HStack>
  )
}

export default SwimlaneNode
