// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

/**
 * @title SimpleToken
 * @dev A basic ERC20 token for learning purposes
 * Deploy this on Sepolia to test token interactions with the API
 */

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract SimpleToken is ERC20, Ownable {
    /**
     * @dev Initialize the token with:
     * - Name: "Learning Token"
     * - Symbol: "LEARN"
     * - Initial supply: 1,000,000 tokens (18 decimals)
     */
    constructor() ERC20("Learning Token", "LEARN") {
        // Mint 1 million tokens to the deployer
        _mint(msg.sender, 1000000 * 10 ** decimals());
    }

    /**
     * @dev Mint new tokens (only owner can call)
     * @param to Address to mint tokens to
     * @param amount Amount of tokens to mint (in base units)
     */
    function mint(address to, uint256 amount) public onlyOwner {
        _mint(to, amount);
    }

    /**
     * @dev Burn tokens from caller's address
     * @param amount Amount of tokens to burn
     */
    function burn(uint256 amount) public {
        _burn(msg.sender, amount);
    }

    /**
     * @dev Burn tokens from a specific address (only owner)
     * @param from Address to burn from
     * @param amount Amount of tokens to burn
     */
    function burnFrom(address from, uint256 amount) public onlyOwner {
        _burn(from, amount);
    }
}

/*
DEPLOYMENT INSTRUCTIONS:

1. Go to https://remix.ethereum.org/
2. Create a new file: SimpleToken.sol
3. Paste this code
4. Install OpenZeppelin dependency:
   - Click Solidity Compiler
   - Make sure compiler version is 0.8.19 or higher
5. Deploy settings:
   - Network: Sepolia (via MetaMask)
   - Make sure you have some Sepolia ETH
6. Click "Deploy"
7. Note the contract address (copy it)
8. Use this address in the API with /api/wallets/{addr}/tokens/add?tokenAddress=...

After deployment, you can:
- Transfer tokens to your wallet
- Test the API's token balance reading
- Learn how ERC20 contracts work
*/

/*
TESTING IN THE API:

1. Deploy this contract on Sepolia
2. Add your wallet to the API
3. Add this token contract to your wallet:
   POST /api/wallets/{your-address}/tokens/add?tokenAddress={contract-address}
4. The API will read:
   - Token name: "Learning Token"
   - Token symbol: "LEARN"
   - Your balance
   - Decimal places: 18
5. Try calling mint() or transfer() in Remix
6. Sync wallet to see updated balance
*/
